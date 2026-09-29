using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Queries;

public record TenantRoleDto(
    string Id,
    string RoleId,
    string Name,
    string Label,
    string? Description,
    string Permissions,
    bool IsCustomized,
    bool IsActive);

public record ListTenantRolesQuery(Guid TenantId) : IRequest<IEnumerable<TenantRoleDto>>;

public class ListTenantRolesQueryHandler(AppDbContext db)
    : IRequestHandler<ListTenantRolesQuery, IEnumerable<TenantRoleDto>>
{
    public async Task<IEnumerable<TenantRoleDto>> Handle(ListTenantRolesQuery request, CancellationToken ct)
    {
        return await db.TenantRoles
            .AsNoTracking()
            .Include(tr => tr.Role)
            .Where(tr => tr.TenantId == request.TenantId && tr.Role.IsActive)
            .OrderBy(tr => tr.Role.Name == "member" ? 99 : 0)
            .ThenBy(tr => tr.Role.Label)
            .Select(tr => new TenantRoleDto(
                tr.Id.ToString(),
                tr.RoleId.ToString(),
                tr.Role.Name,
                tr.Role.Label,
                tr.Role.Description,
                tr.Permissions,
                tr.IsCustomized,
                tr.Role.IsActive))
            .ToListAsync(ct);
    }
}
