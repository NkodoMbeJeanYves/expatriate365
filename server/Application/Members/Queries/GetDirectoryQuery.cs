using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Queries;

public record GetDirectoryQuery(string TenantSlug, string? Search, string? Profession)
    : IRequest<List<DirectoryMemberDto>>;

public class GetDirectoryQueryHandler(AppDbContext db)
    : IRequestHandler<GetDirectoryQuery, List<DirectoryMemberDto>>
{
    public async Task<List<DirectoryMemberDto>> Handle(GetDirectoryQuery req, CancellationToken ct)
    {
        var query = db.Members
            .Include(m => m.User)
            .Include(m => m.Tenant)
            .Where(m =>
                m.Tenant.Slug == req.TenantSlug &&
                m.IsDirectoryVisible &&
                m.IsActive &&
                m.Status == "active" &&
                m.Profession != null);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            query = query.Where(m =>
                m.User.FirstName.ToLower().Contains(s) ||
                m.User.LastName.ToLower().Contains(s) ||
                m.Profession!.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(req.Profession))
        {
            var p = req.Profession.ToLower();
            query = query.Where(m => m.Profession!.ToLower().Contains(p));
        }

        return await query
            .OrderBy(m => m.User.LastName)
            .ThenBy(m => m.User.FirstName)
            .Select(m => new DirectoryMemberDto(
                m.Id.ToString(),
                m.User.FirstName,
                m.User.LastName,
                m.Profession,
                m.PhotoUrl,
                m.Address,
                m.Tenant.Name,
                m.Tenant.Slug))
            .ToListAsync(ct);
    }
}
