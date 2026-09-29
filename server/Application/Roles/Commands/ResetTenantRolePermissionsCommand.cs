using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Commands;

public record ResetTenantRolePermissionsCommand(Guid TenantId, Guid TenantRoleId) : IRequest<ServiceResult<bool>>;

public class ResetTenantRolePermissionsCommandHandler(AppDbContext db, ILogger<ResetTenantRolePermissionsCommandHandler> log)
    : IRequestHandler<ResetTenantRolePermissionsCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(ResetTenantRolePermissionsCommand request, CancellationToken ct)
    {
        var tenantRole = await db.TenantRoles
            .Include(tr => tr.Role)
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRoleId && tr.TenantId == request.TenantId, ct);

        if (tenantRole is null)
            return ServiceResult<bool>.Failure("Rôle introuvable pour cette association.");

        // Restore global template permissions
        tenantRole.Permissions = tenantRole.Role.Permissions;
        tenantRole.IsCustomized = false;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Tenant {TenantId} reset permissions for role {RoleName} to global defaults",
            request.TenantId, tenantRole.Role.Name);
        return ServiceResult<bool>.Success(true);
    }
}
