using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Queries;

public record ListContributionChargesQuery(
    Guid TenantId,
    int Page = 1,
    int Limit = 20,
    string? MemberId = null,
    string? TypeId = null,
    string? Status = null
) : IRequest<PagedResult<ContributionChargeDto>>;

public class ListContributionChargesQueryHandler(AppDbContext db)
    : IRequestHandler<ListContributionChargesQuery, PagedResult<ContributionChargeDto>>
{
    public async Task<PagedResult<ContributionChargeDto>> Handle(ListContributionChargesQuery request, CancellationToken ct)
    {
        var query = db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .Where(c => c.TenantId == request.TenantId && c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.MemberId) && Guid.TryParse(request.MemberId, out var mid))
            query = query.Where(c => c.MemberId == mid);

        if (!string.IsNullOrWhiteSpace(request.TypeId) && Guid.TryParse(request.TypeId, out var tid))
            query = query.Where(c => c.ContributionTypeId == tid);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.Status == request.Status);

        var total = await query.CountAsync(ct);
        var raw = await query
            .OrderByDescending(c => c.DueDate)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = raw.Select(c => ToDto(c)).ToList();

        return PagedResult<ContributionChargeDto>.Create(items, request.Page, request.Limit, total);
    }

    private static ContributionChargeDto ToDto(Domain.Entities.ContributionCharge c) =>
        new(c.Id.ToString(), c.TenantId.ToString(), c.MemberId.ToString(),
            c.Member != null ? $"{c.Member.User.FirstName} {c.Member.User.LastName}" : null,
            c.Member?.MembershipNumber,
            c.ContributionTypeId.ToString(),
            c.ContributionType?.Name,
            c.DueDate.ToString("yyyy-MM-dd"),
            c.BaseAmount, c.PenaltyAmount, c.WaiverAmount, c.AmountPaid,
            c.TotalDue, c.Balance,
            c.Status, c.IsActive,
            c.CreatedAt.ToString("O"),
            c.UpdatedAt != null ? c.UpdatedAt.Value.ToString("O") : null);
}
