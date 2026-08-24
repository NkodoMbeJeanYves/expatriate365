using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record LoginCommand(LoginRequest Dto) : IRequest<ServiceResult<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Password).NotEmpty();
    }
}

public class LoginCommandHandler(AppDbContext db, JwtService jwt, ILogger<LoginCommandHandler> log)
    : IRequestHandler<LoginCommand, ServiceResult<LoginResponse>>
{
    private async Task<string[]> LoadPermissionsAsync(string roleName, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive, ct);
        if (role is null) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions) ?? []; }
        catch { return []; }
    }

    private async Task<(string? entityType, string? entityId)> ResolveEntityAsync(Guid userId, Guid? tenantId, CancellationToken ct)
    {
        if (tenantId is null) return (null, null);
        var member = await db.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, ct);
        if (member is null) return (null, null);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isBoardMember = await db.BoardMembers.AnyAsync(
            b => b.MemberId == member.Id && b.IsActive
              && b.StartDate <= today && (b.EndDate == null || b.EndDate >= today), ct);
        return (isBoardMember ? "board_member" : "member", member.Id.ToString());
    }

    public async Task<ServiceResult<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        log.LogInformation("Login attempt: {Email}", dto.Email);

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == dto.Email.ToLowerInvariant() && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ServiceResult<LoginResponse>.Failure("Email ou mot de passe incorrect.");

        if (user.Status != "active")
            return ServiceResult<LoginResponse>.Failure("Ce compte est suspendu.");

        var (plain, hash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var permissions = await LoadPermissionsAsync(user.Role, ct);
        var (entityType, entityId) = await ResolveEntityAsync(user.Id, user.TenantId, ct);
        log.LogInformation("Login success: {UserId} role={Role} entityType={EntityType}", user.Id, user.Role, entityType);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user, permissions, entityType, entityId),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user, entityType, entityId)
        ));
    }
}
