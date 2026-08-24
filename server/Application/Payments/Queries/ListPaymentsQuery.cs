using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Payments.Queries;

public record ListPaymentsQuery(
    Guid TenantId,
    int Page,
    int Limit,
    string? MemberId,
    string? Status,
    string? From,
    string? To
) : IRequest<PagedResult<PaymentDto>>;

public class ListPaymentsQueryHandler(AppDbContext db)
    : IRequestHandler<ListPaymentsQuery, PagedResult<PaymentDto>>
{
    public async Task<PagedResult<PaymentDto>> Handle(ListPaymentsQuery request, CancellationToken ct)
    {
        var query = db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
            .Where(p => p.TenantId == request.TenantId && p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.MemberId) && Guid.TryParse(request.MemberId, out var memberId))
            query = query.Where(p => p.MemberId == memberId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.From) && DateOnly.TryParse(request.From, out var from))
            query = query.Where(p => p.PaymentDate >= from);

        if (!string.IsNullOrWhiteSpace(request.To) && DateOnly.TryParse(request.To, out var to))
            query = query.Where(p => p.PaymentDate <= to);

        var total = await query.CountAsync(ct);

        var raw = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = raw.Select(p => ToDto(p)).ToList();

        return PagedResult<PaymentDto>.Create(items, request.Page, request.Limit, total);
    }

    internal static PaymentDto ToDto(Domain.Entities.Payment p) => new(
        p.Id.ToString(), p.TenantId.ToString(), p.MemberId.ToString(),
        $"{p.Member.User.FirstName} {p.Member.User.LastName}", p.Member.MembershipNumber,
        p.ChargeId.ToString(), p.Charge.ContributionType.Name,
        p.ReceiptNumber, p.Amount, p.Currency,
        p.PaymentGateway ?? "cash", p.Notes, p.ReceiptFileUrl, p.Status,
        p.PaymentDate.ToString("yyyy-MM-dd"),
        p.ConfirmedAt?.ToString("O"), p.ReversedAt?.ToString("O"), p.ReversalReason,
        p.CreatedAt.ToString("O"), p.UpdatedAt?.ToString("O"));
}
