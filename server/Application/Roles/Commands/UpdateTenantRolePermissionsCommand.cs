using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Commands;

public record UpdateTenantRolePermissionsCommand(Guid TenantId, Guid TenantRoleId, UpdateRolePermissionsRequest Dto)
    : IRequest<ServiceResult<bool>>;

public class UpdateTenantRolePermissionsCommandHandler(AppDbContext db, ILogger<UpdateTenantRolePermissionsCommandHandler> log)
    : IRequestHandler<UpdateTenantRolePermissionsCommand, ServiceResult<bool>>
{
    private static readonly HashSet<string> ValidPermissions = new(Permissions.All);

    public async Task<ServiceResult<bool>> Handle(UpdateTenantRolePermissionsCommand request, CancellationToken ct)
    {
        var tenantRole = await db.TenantRoles
            .Include(tr => tr.Role)
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRoleId && tr.TenantId == request.TenantId, ct);

        if (tenantRole is null)
            return ServiceResult<bool>.Failure("Rôle introuvable pour cette association.", "errors.role.not_found");

        var invalid = request.Dto.Permissions.Where(p => !ValidPermissions.Contains(p)).ToList();
        if (invalid.Count > 0)
            return ServiceResult<bool>.Failure($"Permissions inconnues : {string.Join(", ", invalid)}", "errors.role.unknown_permissions");

        tenantRole.Permissions = JsonSerializer.Serialize(request.Dto.Permissions.Distinct().ToArray());
        tenantRole.IsCustomized = true;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Tenant {TenantId} updated permissions for role {RoleName}: {Count} permissions",
            request.TenantId, tenantRole.Role.Name, request.Dto.Permissions.Length);
        return ServiceResult<bool>.Success(true);
    }
}
