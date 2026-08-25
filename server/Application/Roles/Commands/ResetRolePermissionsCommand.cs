using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Commands;

public record ResetRolePermissionsCommand(Guid RoleId) : IRequest<ServiceResult<bool>>;

public class ResetRolePermissionsCommandHandler(AppDbContext db, ILogger<ResetRolePermissionsCommandHandler> log)
    : IRequestHandler<ResetRolePermissionsCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(ResetRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
            return ServiceResult<bool>.Failure("Rôle introuvable.");

        var defaults = RoleSeeder.GetDefaultPermissions(role.Name);
        if (defaults is null)
            return ServiceResult<bool>.Failure("Aucune configuration par défaut pour ce rôle.");

        role.Permissions  = defaults;
        role.IsCustomized = false;
        role.UpdatedAt    = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Permissions reset to defaults for role {RoleName}", role.Name);
        return ServiceResult<bool>.Success(true);
    }
}
