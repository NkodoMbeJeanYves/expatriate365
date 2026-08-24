using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Queries;

public record GetContributionStatsQuery(Guid TenantId, string? TypeId = null, Guid? MemberId = null)
    : IRequest<ContributionStatsDto>;

public class GetContributionStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetContributionStatsQuery, ContributionStatsDto>
{
    public async Task<ContributionStatsDto> Handle(GetContributionStatsQuery request, CancellationToken ct)
    {
        var query = db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId && c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.TypeId) && Guid.TryParse(request.TypeId, out var tid))
            query = query.Where(c => c.ContributionTypeId == tid);
        if (request.MemberId.HasValue)
            query = query.Where(c => c.MemberId == request.MemberId.Value);

        var raw = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalCharges   = g.Count(),
                PaidCount      = g.Count(c => c.Status == "paid"),
                PendingCount   = g.Count(c => c.Status == "pending"),
                OverdueCount   = g.Count(c => c.Status == "overdue"),
                WaivedCount    = g.Count(c => c.Status == "waived"),
                TotalExpected  = g.Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount),
                TotalCollected = g.Sum(c => c.AmountPaid),
                TotalPending   = g.Where(c => c.Status != "paid" && c.Status != "waived")
                                  .Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount - c.AmountPaid),
            })
            .FirstOrDefaultAsync(ct);

        return raw is null
            ? new ContributionStatsDto(0, 0, 0, 0, 0, 0, 0, 0)
            : new ContributionStatsDto(raw.TotalCharges, raw.PaidCount, raw.PendingCount, raw.OverdueCount,
                                       raw.WaivedCount, raw.TotalExpected, raw.TotalCollected, raw.TotalPending);
    }
}
