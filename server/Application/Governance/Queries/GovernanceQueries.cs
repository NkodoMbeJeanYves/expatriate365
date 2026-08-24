using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Governance.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Governance.Queries;

public record ListBoardMembersQuery(Guid TenantId) : IRequest<List<BoardMemberDto>>;

public class ListBoardMembersQueryHandler(AppDbContext db)
    : IRequestHandler<ListBoardMembersQuery, List<BoardMemberDto>>
{
    public async Task<List<BoardMemberDto>> Handle(ListBoardMembersQuery request, CancellationToken ct)
    {
        return await db.BoardMembers
            .Where(b => b.TenantId == request.TenantId && b.IsActive)
            .OrderBy(b => b.StartDate)
            .Select(b => new BoardMemberDto(
                b.Id.ToString(), b.TenantId.ToString(),
                b.MemberId.ToString(),
                b.Member.User.FirstName + " " + b.Member.User.LastName,
                b.Member.MembershipNumber,
                b.Role, b.StartDate.ToString(), b.EndDate != null ? b.EndDate.Value.ToString() : null,
                b.Notes, b.CreatedAt.ToString(), b.UpdatedAt != null ? b.UpdatedAt.Value.ToString() : null))
            .ToListAsync(ct);
    }
}

public record ListResolutionsQuery(Guid TenantId, int Page, int Limit, string? Status)
    : IRequest<PagedResult<ResolutionDto>>;

public class ListResolutionsQueryHandler(AppDbContext db)
    : IRequestHandler<ListResolutionsQuery, PagedResult<ResolutionDto>>
{
    public async Task<PagedResult<ResolutionDto>> Handle(ListResolutionsQuery request, CancellationToken ct)
    {
        var query = db.Resolutions
            .Where(r => r.TenantId == request.TenantId && r.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        return PagedResult<ResolutionDto>.Create(items.Select(ToDto).ToList(), request.Page, request.Limit, total);
    }

    internal static ResolutionDto ToDto(Domain.Entities.Resolution r) => new(
        r.Id.ToString(), r.TenantId.ToString(), r.Title, r.Content, r.Status,
        r.MeetingId, r.AdoptedAt?.ToString("O"),
        r.VotesFor, r.VotesAgainst, r.Abstentions,
        r.CreatedAt.ToString("O"), r.UpdatedAt?.ToString("O"));
}

public record GetGovernanceStatsQuery(Guid TenantId) : IRequest<GovernanceStatsDto>;

public class GetGovernanceStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetGovernanceStatsQuery, GovernanceStatsDto>
{
    public async Task<GovernanceStatsDto> Handle(GetGovernanceStatsQuery request, CancellationToken ct)
    {
        var boardCount = await db.BoardMembers.CountAsync(b => b.TenantId == request.TenantId && b.IsActive, ct);

        var resStats = await db.Resolutions
            .Where(r => r.TenantId == request.TenantId && r.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Adopted = g.Count(r => r.Status == "adopted") })
            .FirstOrDefaultAsync(ct);

        return new GovernanceStatsDto(boardCount, resStats?.Total ?? 0, resStats?.Adopted ?? 0);
    }
}
