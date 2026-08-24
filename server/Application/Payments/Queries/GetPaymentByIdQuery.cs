using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Payments.Queries;

public record GetPaymentByIdQuery(Guid TenantId, Guid Id) : IRequest<ServiceResult<PaymentDto>>;

public class GetPaymentByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetPaymentByIdQuery, ServiceResult<PaymentDto>>
{
    public async Task<ServiceResult<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == request.TenantId, ct);

        if (payment is null)
            return ServiceResult<PaymentDto>.Failure("Paiement introuvable.");

        return ServiceResult<PaymentDto>.Success(ListPaymentsQueryHandler.ToDto(payment));
    }
}

public record GetPaymentStatsQuery(Guid TenantId, Guid? MemberId = null) : IRequest<PaymentStatsDto>;

public class GetPaymentStatsQueryHandler(AppDbContext db)
    : IRequestHandler<GetPaymentStatsQuery, PaymentStatsDto>
{
    public async Task<PaymentStatsDto> Handle(GetPaymentStatsQuery request, CancellationToken ct)
    {
        var payments = await db.Payments
            .Where(p => p.TenantId == request.TenantId && p.IsActive)
            .Where(p => request.MemberId == null || p.MemberId == request.MemberId)
            .Select(p => new { p.Status, p.Amount })
            .ToListAsync(ct);

        return new PaymentStatsDto(
            TotalCount: payments.Count,
            ConfirmedCount: payments.Count(p => p.Status == "confirmed"),
            PendingCount: payments.Count(p => p.Status == "pending"),
            ReversedCount: payments.Count(p => p.Status == "reversed"),
            TotalConfirmed: payments.Where(p => p.Status == "confirmed").Sum(p => p.Amount),
            TotalPending: payments.Where(p => p.Status == "pending").Sum(p => p.Amount)
        );
    }
}
