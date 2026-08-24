using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Queries;

public record GetMembersQuery(
    Guid TenantId,
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    string? Status = null,
    string? CategoryId = null
) : IRequest<PagedResult<MemberListItemDto>>;

public class GetMembersQueryHandler(AppDbContext db)
    : IRequestHandler<GetMembersQuery, PagedResult<MemberListItemDto>>
{
    public async Task<PagedResult<MemberListItemDto>> Handle(GetMembersQuery q, CancellationToken ct)
    {
        var query = db.Members
            .Include(m => m.User)
            .Include(m => m.Category)
            .Where(m => m.TenantId == q.TenantId && m.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(m =>
                m.User.FirstName.ToLower().Contains(s) ||
                m.User.LastName.ToLower().Contains(s) ||
                m.User.Email.ToLower().Contains(s) ||
                m.MembershipNumber.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(m => m.Status == q.Status);

        if (!string.IsNullOrWhiteSpace(q.CategoryId) && Guid.TryParse(q.CategoryId, out var catId))
            query = query.Where(m => m.CategoryId == catId);

        var total = await query.CountAsync(ct);
        var raw = await query
            .OrderBy(m => m.User.LastName).ThenBy(m => m.User.FirstName)
            .Skip((q.Page - 1) * q.Limit).Take(q.Limit)
            .ToListAsync(ct);

        var items = raw.Select(m => new MemberListItemDto(
            m.Id.ToString(),
            m.MembershipNumber,
            m.User.FirstName,
            m.User.LastName,
            m.User.Email,
            m.User.Phone,
            m.Status,
            m.CategoryId?.ToString(),
            m.Category?.Name,
            m.JoinedDate.ToString("yyyy-MM-dd"),
            m.PhotoUrl,
            m.IsActive
        )).ToList();

        return PagedResult<MemberListItemDto>.Create(items, q.Page, q.Limit, total);
    }
}
