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
        DateOnly? to   = request.To   is not null ? DateOnly.Parse(request.To)   : null;

        // Project only needed columns — EF Core translates navigation access to JOINs, no Include needed
        var paymentsQ = db.Payments
            .Where(p => p.TenantId == request.TenantId && p.IsActive)
            .Where(p => request.Status == null || p.Status == request.Status)
            .Where(p => from == null || p.PaymentDate >= from)
            .Where(p => to   == null || p.PaymentDate <= to)
            .Select(p => new TransactionRow(
                p.Id, "contribution",
                p.Member.User.FirstName + " " + p.Member.User.LastName,
                p.Member.MembershipNumber,
                p.Amount, p.Currency, p.Status,
                p.PaymentDate, p.Notes));

        var welfareQ = db.WelfareRequests
            .Where(w => w.TenantId == request.TenantId && w.IsActive
                     && w.AmountPaid.HasValue && w.AmountPaid > 0)
            .Where(w => from == null || (w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) >= from))
            .Where(w => to   == null || (w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) <= to))
            .Select(w => new TransactionRow(
                w.Id, "welfare",
                w.Member.User.FirstName + " " + w.Member.User.LastName,
                w.Member.MembershipNumber,
                -(w.AmountPaid ?? 0), "EUR", "paid",
                w.PaidAt.HasValue ? DateOnly.FromDateTime(w.PaidAt.Value) : DateOnly.MinValue,
                w.Type));

        // Fetch only the filtered rows (not the full entity graph)
        var payments = request.Type == "welfare"  ? [] : await paymentsQ.ToListAsync(ct);
        var welfare  = request.Type == "contribution" ? [] : await welfareQ.ToListAsync(ct);

        var sorted = payments.Concat(welfare).OrderByDescending(t => t.Date).ToList();
        var total  = sorted.Count;
        var paged  = sorted
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(t => new FinanceTransactionDto(
                t.Id.ToString(), t.Type, t.MemberName, t.MembershipNumber,
                t.Amount, t.Currency, t.Status, t.Date.ToString("O"), t.Notes))
            .ToList();

        return PagedResult<FinanceTransactionDto>.Create(paged, request.Page, request.Limit, total);
    }

    private record TransactionRow(
        Guid Id, string Type, string MemberName, string MembershipNumber,
        decimal Amount, string Currency, string Status, DateOnly Date, string? Notes);
}
