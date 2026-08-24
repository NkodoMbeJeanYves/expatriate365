using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Queries;

public record ListElectionsQuery(
    Guid TenantId, Guid? VoterId, int Page, int Limit, string? Status, string? Type
) : IRequest<PagedResult<ElectionDto>>;

public class ListElectionsQueryHandler(AppDbContext db)
    : IRequestHandler<ListElectionsQuery, PagedResult<ElectionDto>>
{
    public async Task<PagedResult<ElectionDto>> Handle(ListElectionsQuery request, CancellationToken ct)
    {
        var query = db.Elections
            .Include(e => e.Candidates)
            .Include(e => e.Votes)
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(e => e.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(e => e.Type == request.Type);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        // Batch-check which elections the current voter has already voted in
        HashSet<Guid> votedElectionIds = [];
        if (request.VoterId.HasValue && items.Count > 0)
        {
            var electionIds = items.Select(e => e.Id).ToList();
            votedElectionIds = (await db.ElectionVotes
                .Where(v => v.VoterId == request.VoterId.Value && electionIds.Contains(v.ElectionId) && v.IsActive)
                .Select(v => v.ElectionId)
                .ToListAsync(ct)).ToHashSet();
        }

        return PagedResult<ElectionDto>.Create(
            items.Select(e => ToDto(e, votedElectionIds.Contains(e.Id))).ToList(),
            request.Page, request.Limit, total);
    }

    internal static ElectionDto ToDto(Domain.Entities.Election e, bool hasVoted = false) => new(
        e.Id.ToString(), e.TenantId.ToString(), e.Title, e.Description,
        e.Type, e.Status,
        e.StartDate?.ToString("O"), e.EndDate?.ToString("O"),
        e.MaxChoices,
        e.Candidates.Count(c => c.IsActive),
        e.Votes.Count(v => v.IsActive),
        e.CreatedAt.ToString("O"), e.UpdatedAt?.ToString("O"),
        hasVoted);
}
