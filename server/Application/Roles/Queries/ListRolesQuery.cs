using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;

namespace server.Application.Roles.Queries;

public record RoleDto(string Id, string Name, string Label, string? Description, string Permissions, bool IsActive, bool IsCustomized);

public record ListRolesQuery : IRequest<IEnumerable<RoleDto>>;

public class ListRolesQueryHandler(AppDbContext db)
    : IRequestHandler<ListRolesQuery, IEnumerable<RoleDto>>
{
    public async Task<IEnumerable<RoleDto>> Handle(ListRolesQuery request, CancellationToken ct)
    {
        return await db.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name == "member" ? 99 : 0)  // member last
            .ThenBy(r => r.Label)
            .Select(r => new RoleDto(r.Id.ToString(), r.Name, r.Label, r.Description, r.Permissions, r.IsActive, r.IsCustomized))
            .ToListAsync(ct);
    }
}
