using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Finances.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Finances.Queries;

public record GetFinanceSummaryQuery(Guid TenantId) : IRequest<FinanceSummaryDto>;

public class GetFinanceSummaryQueryHandler(AppDbContext db)
    : IRequestHandler<GetFinanceSummaryQuery, FinanceSummaryDto>
{
    public async Task<FinanceSummaryDto> Handle(GetFinanceSummaryQuery request, CancellationToken ct)
    {
        var charges = await db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId && c.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Collected = g.Sum(c => c.AmountPaid),
                Expected = g.Sum(c => c.BaseAmount + c.PenaltyAmount - c.WaiverAmount),
            })
            .FirstOrDefaultAsync(ct);

        var txCount = await db.Payments.CountAsync(p => p.TenantId == request.TenantId && p.IsActive, ct);

        var collected = charges?.Collected ?? 0;
        var expected = charges?.Expected ?? 0;
        var rate = expected > 0 ? Math.Round(collected / expected * 100, 1) : 0;

        return new FinanceSummaryDto(collected, expected, collected - expected, rate, txCount);
    }
}

public record ListFinanceTransactionsQuery(
    Guid TenantId, int Page, int Limit, string? Type, string? Status, string? From, string? To
) : IRequest<PagedResult<FinanceTransactionDto>>;

public class ListFinanceTransactionsQueryHandler(AppDbContext db)
    : IRequestHandler<ListFinanceTransactionsQuery, PagedResult<FinanceTransactionDto>>
{
    public async Task<PagedResult<FinanceTransactionDto>> Handle(
        ListFinanceTransactionsQuery request, CancellationToken ct)
    {
        DateOnly? from = request.From is not null ? DateOnly.Parse(request.From) : null;
        DateOnly? to = request.To is not null ? DateOnly.Parse(request.To) : null;

        // Payments (cotisations)
        var paymentsQuery = db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Where(p => p.TenantId == request.TenantId && p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            paymentsQuery = paymentsQuery.Where(p => p.Status == request.Status);
        if (from.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= to.Value);

        var rawPayments = await paymentsQuery.ToListAsync(ct);
        var payments = rawPayments.Select(p => new FinanceTransactionDto(
                p.Id.ToString(), "contribution",
                $"{p.Member.User.FirstName} {p.Member.User.LastName}",
                p.Member.MembershipNumber,
                p.Amount, p.Currency, p.Status,
                p.PaymentDate.ToString("O"), p.Notes)).ToList();

        // Welfare payments
        var welfareQuery = db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .Where(w => w.TenantId == request.TenantId && w.IsActive
                && w.AmountPaid.HasValue && w.AmountPaid > 0);

        if (from.HasValue)
            welfareQuery = welfareQuery.Where(w => w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) >= from.Value);
        if (to.HasValue)
            welfareQuery = welfareQuery.Where(w => w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) <= to.Value);

        var rawWelfare = await welfareQuery.ToListAsync(ct);
        var welfare = rawWelfare.Select(w => new FinanceTransactionDto(
                w.Id.ToString(), "welfare",
                $"{w.Member.User.FirstName} {w.Member.User.LastName}",
                w.Member.MembershipNumber,
                -(w.AmountPaid ?? 0), "EUR", "paid",
                w.PaidAt!.Value.ToString("O"), w.Type)).ToList();

        // Filter by type
        IEnumerable<FinanceTransactionDto> all = request.Type switch
        {
            "contribution" => payments,
            "welfare" => welfare,
            _ => payments.Concat(welfare),
        };

        var sorted = all.OrderByDescending(t => t.Date).ToList();
        var total = sorted.Count;
        var paged = sorted.Skip((request.Page - 1) * request.Limit).Take(request.Limit).ToList();

        return PagedResult<FinanceTransactionDto>.Create(paged, request.Page, request.Limit, total);
    }
}
