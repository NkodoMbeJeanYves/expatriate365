using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ServiceResult<LoginResponse>>;

public class RefreshTokenCommandHandler(AppDbContext db, JwtService jwt, ILogger<RefreshTokenCommandHandler> log)
    : IRequestHandler<RefreshTokenCommand, ServiceResult<LoginResponse>>
{
    private async Task<string[]> LoadPermissionsAsync(string roleName, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive, ct);
        if (role is null) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions) ?? []; }
        catch { return []; }
    }

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

    public async Task<ServiceResult<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = JwtService.HashToken(request.RefreshToken);
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.RefreshTokenHash == hash && u.IsActive, ct);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            log.LogWarning("Refresh token invalid or expired");
            return ServiceResult<LoginResponse>.Failure("Refresh token invalide ou expiré.");
        }

        var (plain, newHash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = newHash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        await db.SaveChangesAsync(ct);

        var permissions = await LoadPermissionsAsync(user.Role, ct);
        var (entityType, entityId) = await ResolveEntityAsync(user.Id, user.TenantId, user.Role, ct);
        log.LogInformation("Token refreshed for user {UserId} entityType={EntityType}", user.Id, entityType);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user, permissions, entityType, entityId),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user, entityType, entityId)
        ));
    }
}
