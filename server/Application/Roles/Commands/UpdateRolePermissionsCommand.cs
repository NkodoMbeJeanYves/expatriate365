using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Commands;

public record UpdateRolePermissionsRequest(string[] Permissions);

public record UpdateRolePermissionsCommand(Guid RoleId, UpdateRolePermissionsRequest Dto)
    : IRequest<ServiceResult<bool>>;

public class UpdateRolePermissionsCommandHandler(AppDbContext db, ILogger<UpdateRolePermissionsCommandHandler> log)
    : IRequestHandler<UpdateRolePermissionsCommand, ServiceResult<bool>>
{
    private static readonly HashSet<string> ValidPermissions =
        new(Application.Common.Permissions.All);

    public async Task<ServiceResult<bool>> Handle(UpdateRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
            return ServiceResult<bool>.Failure("Rôle introuvable.");

        var invalid = request.Dto.Permissions.Where(p => !ValidPermissions.Contains(p)).ToList();
        if (invalid.Count > 0)
            return ServiceResult<bool>.Failure($"Permissions inconnues : {string.Join(", ", invalid)}");

        role.Permissions = JsonSerializer.Serialize(request.Dto.Permissions.Distinct().ToArray());
        role.UpdatedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Permissions updated for role {RoleName}: {Count} permissions", role.Name, request.Dto.Permissions.Length);
        return ServiceResult<bool>.Success(true);
    }
}
