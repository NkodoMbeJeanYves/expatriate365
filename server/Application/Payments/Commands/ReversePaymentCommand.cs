using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Application.Payments.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Payments.Commands;

public record ReversePaymentCommand(Guid TenantId, Guid PaymentId, Guid ReversedBy, ReversePaymentRequest Dto)
    : IRequest<ServiceResult<PaymentDto>>;

public class ReversePaymentValidator : AbstractValidator<ReversePaymentCommand>
{
    public ReversePaymentValidator()
    {
        RuleFor(x => x.Dto.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ReversePaymentCommandHandler(AppDbContext db, ILogger<ReversePaymentCommandHandler> log)
    : IRequestHandler<ReversePaymentCommand, ServiceResult<PaymentDto>>
{
    public async Task<ServiceResult<PaymentDto>> Handle(ReversePaymentCommand request, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.TenantId == request.TenantId, ct);

        if (payment is null) return ServiceResult<PaymentDto>.Failure("Paiement introuvable.");
        if (payment.Status == "reversed") return ServiceResult<PaymentDto>.Failure("Ce paiement est déjà annulé.");

        // Roll back charge amount
        var charge = payment.Charge;
        charge.AmountPaid = Math.Max(0, charge.AmountPaid - payment.Amount);
        charge.Status = charge.AmountPaid >= charge.TotalDue ? "paid" : "pending";

        payment.Status = "reversed";
        payment.ReversedAt = DateTime.UtcNow;
        payment.ReversedBy = request.ReversedBy;
        payment.ReversalReason = request.Dto.Reason;
        payment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Payment {Id} reversed: {Reason}", payment.Id, request.Dto.Reason);

        return ServiceResult<PaymentDto>.Success(ListPaymentsQueryHandler.ToDto(payment));
    }
}
