using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Queries;

public record ListElectionsQuery(
    Guid TenantId, Guid UserId, int Page, int Limit, string? Status, string? Type
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

        // Single query to get all election IDs the current user has voted in
        var electionIds = items.Select(e => e.Id).ToList();
        var votedIds = await db.ElectionVotes
            .Where(v => v.VoterId == request.UserId && electionIds.Contains(v.ElectionId) && v.IsActive)
            .Select(v => v.ElectionId)
            .ToHashSetAsync(ct);

        return PagedResult<ElectionDto>.Create(
            items.Select(e => ToDto(e, votedIds.Contains(e.Id))).ToList(),
            request.Page, request.Limit, total);
    }

    internal static ElectionDto ToDto(Domain.Entities.Election e, bool hasVoted = false) => new(
        e.Id.ToString(), e.TenantId.ToString(), e.Title, e.Description,
        e.Type, e.Status,
        e.StartDate?.ToString("O"), e.EndDate?.ToString("O"),
        e.MaxChoices,
        e.Candidates.Count(c => c.IsActive),
        e.Votes.Count(v => v.IsActive),
        hasVoted,
        e.CreatedAt.ToString("O"), e.UpdatedAt?.ToString("O"));
}
