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

public class LoginCommandHandler(AppDbContext db, JwtService jwt, ILogger<LoginCommandHandler> log, AuditService audit, PermissionResolverService permissionResolver)
    : IRequestHandler<LoginCommand, ServiceResult<LoginResponse>>
{

    private async Task<(string? entityType, string? entityId)> ResolveEntityAsync(Guid userId, Guid? tenantId, string role, CancellationToken ct)
    {
        if (tenantId is null) return (role, userId.ToString());
        var member = await db.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, ct);
        if (member is null) return (role, userId.ToString());
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

        if (user.TenantId.HasValue)
        {
            var tenant = await db.Tenants.FindAsync([user.TenantId.Value], ct);
            if (tenant is null || !tenant.IsActive)
            {
                log.LogWarning("Login blocked: tenant {TenantId} is inactive for user {UserId}", user.TenantId, user.Id);
                return ServiceResult<LoginResponse>.Failure("Cette association est désactivée. Contactez votre administrateur.");
            }
        }

        var (plain, hash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        user.LastLoginAt = DateTime.UtcNow;
        audit.Log("login", user.Id, user.TenantId);
        await db.SaveChangesAsync(ct);

        var permissions = await permissionResolver.ResolveAsync(user.Role, user.TenantId, ct);
        var (entityType, entityId) = await ResolveEntityAsync(user.Id, user.TenantId, user.Role, ct);
        log.LogInformation("Login success: {UserId} role={Role} entityType={EntityType}", user.Id, user.Role, entityType);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user, permissions, entityType, entityId),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user, entityType, entityId)
        ));
    }
}
