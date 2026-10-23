using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Auth.DTOs;
using server.Application.Common;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Auth.Queries;

public record GetMeQuery(Guid UserId, Guid? TenantId = null) : IRequest<ServiceResult<MeResponse>>;

public class GetMeQueryHandler(AppDbContext db, PermissionResolverService permissionResolver,
    ILogger<GetMeQueryHandler> log) : IRequestHandler<GetMeQuery, ServiceResult<MeResponse>>
{
    public async Task<ServiceResult<MeResponse>> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (user is null) return ServiceResult<MeResponse>.Failure("Utilisateur introuvable.", "errors.user.not_found");

        var tenantId = user.Role == "super_admin" ? request.TenantId ?? user.TenantId : user.TenantId;
        var perms = await permissionResolver.ResolveAsync(user.Role, tenantId, ct);
        var (entityType, entityId) = user.Role == "super_admin" && request.TenantId.HasValue
            ? ("super_admin", user.Id.ToString())
            : await AuthEntityResolver.ResolveAsync(db, user.Id, tenantId, user.Role, ct);
        log.LogInformation("Loaded session profile for user {UserId} in tenant {TenantId}", user.Id, tenantId);
        return ServiceResult<MeResponse>.Success(new MeResponse(
            user.Id.ToString(),
            user.Email,
            user.FullName,
            [user.Role],
            tenantId?.ToString(),
            entityType,
            entityId,
            user.EmailVerifiedAt?.ToString("O"),
            perms,
            user.PreferredLanguage
        ));
    }
}
