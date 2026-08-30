using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Commands;

public record SelectTenantCommand(Guid UserId, Guid TenantId) : IRequest<ServiceResult<LoginResponse>>;

public class SelectTenantCommandHandler(AppDbContext db, JwtService jwt, ILogger<SelectTenantCommandHandler> log)
    : IRequestHandler<SelectTenantCommand, ServiceResult<LoginResponse>>
{
    public async Task<ServiceResult<LoginResponse>> Handle(SelectTenantCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (user is null)
            return ServiceResult<LoginResponse>.Failure("Utilisateur introuvable.");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive, ct);
        if (tenant is null)
            return ServiceResult<LoginResponse>.Failure("Communauté introuvable ou inactive.");

        var (plain, hash) = jwt.GenerateRefreshToken();
        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = jwt.RefreshTokenExpiry();
        await db.SaveChangesAsync(ct);

        var role = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == user.Role && r.IsActive, ct);
        var permissions = role is null ? [] :
            (System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions) ?? []);

        log.LogInformation("SuperAdmin {UserId} selected tenant {TenantId}", user.Id, request.TenantId);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(
            jwt.GenerateAccessToken(user, permissions, "super_admin", user.Id.ToString(), request.TenantId),
            plain,
            jwt.AccessExpirySeconds,
            jwt.ToUserInfo(user, "super_admin", user.Id.ToString(), request.TenantId)
        ));
    }
}
