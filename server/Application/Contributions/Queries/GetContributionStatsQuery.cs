using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Queries;

public record GetContributionStatsQuery(Guid TenantId, string? TypeId = null)
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

        var charges = await query.ToListAsync(ct);

        return new ContributionStatsDto(
            TotalCharges: charges.Count,
            PaidCount: charges.Count(c => c.Status == "paid"),
            PendingCount: charges.Count(c => c.Status == "pending"),
            OverdueCount: charges.Count(c => c.Status == "overdue"),
            WaivedCount: charges.Count(c => c.Status == "waived"),
            TotalExpected: charges.Sum(c => c.TotalDue),
            TotalCollected: charges.Sum(c => c.AmountPaid),
            TotalPending: charges.Where(c => c.Status != "paid" && c.Status != "waived").Sum(c => c.Balance)
        );
    }
}
