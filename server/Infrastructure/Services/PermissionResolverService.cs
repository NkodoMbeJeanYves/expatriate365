using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

/// <summary>
/// Resolves the effective permissions for a user role within a tenant.
/// Checks tenant_roles first (tenant-specific override), falls back to the global roles table.
/// </summary>
public class PermissionResolverService(AppDbContext db)
{
    public async Task<string[]> ResolveAsync(string roleName, Guid? tenantId, CancellationToken ct = default)
    {
        var role = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive, ct);
        if (role is null) return [];

        if (tenantId.HasValue)
        {
            var tenantRole = await db.TenantRoles.AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TenantId == tenantId.Value && tr.RoleId == role.Id, ct);
            if (tenantRole is not null)
                return Deserialize(tenantRole.Permissions);
        }

        return Deserialize(role.Permissions);
    }

    public async Task SeedForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var roles = await db.Roles.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var existingRoleIds = await db.TenantRoles
            .Where(tr => tr.TenantId == tenantId)
            .Select(tr => tr.RoleId)
            .ToListAsync(ct);

        var toAdd = roles
            .Where(r => !existingRoleIds.Contains(r.Id))
            .Select(r => new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoleId = r.Id,
                Permissions = r.Permissions,
                IsCustomized = false,
            });

        db.TenantRoles.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
    }

    private static string[] Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}
