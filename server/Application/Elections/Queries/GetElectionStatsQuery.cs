using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Elections.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Queries;

public record GetElectionStatsQuery(Guid TenantId) : IRequest<ElectionStatsDto>;

public class GetElectionStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetElectionStatsQuery, ElectionStatsDto>
{
    public async Task<ElectionStatsDto> Handle(GetElectionStatsQuery request, CancellationToken ct)
    {
        var stats = await db.Elections
            .Where(e => e.TenantId == request.TenantId && e.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Draft = g.Count(e => e.Status == "draft"),
                Open = g.Count(e => e.Status == "open"),
                Closed = g.Count(e => e.Status == "closed"),
                ResultsPublished = g.Count(e => e.Status == "results_published"),
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new ElectionStatsDto(0, 0, 0, 0, 0)
            : new ElectionStatsDto(stats.Total, stats.Draft, stats.Open, stats.Closed, stats.ResultsPublished);
    }
}
