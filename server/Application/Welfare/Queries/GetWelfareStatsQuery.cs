using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Welfare.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Queries;

public record GetWelfareStatsQuery(Guid TenantId) : IRequest<WelfareStatsDto>;

public class GetWelfareStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetWelfareStatsQuery, WelfareStatsDto>
{
    public async Task<WelfareStatsDto> Handle(GetWelfareStatsQuery request, CancellationToken ct)
    {
        var items = await db.WelfareRequests
            .Where(w => w.TenantId == request.TenantId && w.IsActive)
            .Select(w => new { w.Status, w.AmountRequested, w.AmountApproved, w.AmountPaid })
            .ToListAsync(ct);

        return new WelfareStatsDto(
            TotalCount: items.Count,
            PendingCount: items.Count(w => w.Status == "pending"),
            ApprovedCount: items.Count(w => w.Status == "approved"),
            RejectedCount: items.Count(w => w.Status == "rejected"),
            PaidCount: items.Count(w => w.Status == "paid"),
            TotalRequested: items.Sum(w => w.AmountRequested),
            TotalApproved: items.Where(w => w.AmountApproved.HasValue).Sum(w => w.AmountApproved!.Value),
            TotalPaid: items.Where(w => w.AmountPaid.HasValue).Sum(w => w.AmountPaid!.Value)
        );
    }
}
