using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Application.Payments.Queries;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Payments.Commands;

public record ConfirmPaymentCommand(Guid TenantId, Guid PaymentId, Guid ConfirmedBy)
    : IRequest<ServiceResult<PaymentDto>>;

public class ConfirmPaymentCommandHandler(AppDbContext db, ILogger<ConfirmPaymentCommandHandler> log, INotificationService notif)
    : IRequestHandler<ConfirmPaymentCommand, ServiceResult<PaymentDto>>
{
    public async Task<ServiceResult<PaymentDto>> Handle(ConfirmPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.TenantId == request.TenantId, ct);

        if (payment is null) return ServiceResult<PaymentDto>.Failure("Paiement introuvable.");
        if (payment.Status == "confirmed") return ServiceResult<PaymentDto>.Failure("Ce paiement est déjà confirmé.");
        if (payment.Status == "reversed") return ServiceResult<PaymentDto>.Failure("Ce paiement a été annulé.");

        payment.Status = "confirmed";
        payment.ConfirmedAt = DateTime.UtcNow;
        payment.ConfirmedBy = request.ConfirmedBy;
        payment.UpdatedAt = DateTime.UtcNow;

        // Recalculate charge status based on all confirmed payments
        var confirmedTotal = await db.Payments
            .Where(p => p.ChargeId == payment.ChargeId && p.Status == "confirmed" && p.Id != payment.Id)
            .SumAsync(p => p.Amount, ct);
        confirmedTotal += payment.Amount;
        var charge = payment.Charge;
        charge.AmountPaid = confirmedTotal;
        charge.Status = confirmedTotal >= charge.TotalDue ? "paid" : "pending";
        charge.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Payment {Id} confirmed by {UserId}", payment.Id, request.ConfirmedBy);

        // Notify the member
        var memberName = $"{payment.Member.User.FirstName} {payment.Member.User.LastName}";
        await notif.NotifyWithEmailAsync(
            tenantId: payment.TenantId,
            userId: payment.Member.UserId,
            type: "payment_confirmed",
            title: "Paiement confirmé",
            body: $"{payment.Charge.ContributionType.Name} — {payment.Amount:N0} FCFA — reçu {payment.ReceiptNumber}",
            emailSubject: "Votre paiement a été confirmé",
            emailHtml: EmailTemplates.PaymentConfirmed(memberName, payment.Charge.ContributionType.Name, payment.Amount, payment.ReceiptNumber),
            ct);

        return ServiceResult<PaymentDto>.Success(ListPaymentsQueryHandler.ToDto(payment));
    }
}
