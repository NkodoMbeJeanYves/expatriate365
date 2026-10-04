using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Payments.Commands;

public record RecordPaymentCommand(Guid TenantId, RecordPaymentRequest Dto)
    : IRequest<ServiceResult<PaymentDto>>;

public class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.Dto.ChargeId).NotEmpty();
        RuleFor(x => x.Dto.Amount).GreaterThan(0);
        RuleFor(x => x.Dto.PaymentDate).NotEmpty();
        RuleFor(x => x.Dto.PaymentMethod).NotEmpty();
    }
}

public class RecordPaymentCommandHandler(AppDbContext db, ILogger<RecordPaymentCommandHandler> log, INotificationService notif)
    : IRequestHandler<RecordPaymentCommand, ServiceResult<PaymentDto>>
{
    private static readonly string[] ValidMethods = ["cash", "bank_transfer", "mobile_money", "card", "cheque"];

    public async Task<ServiceResult<PaymentDto>> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (!Guid.TryParse(dto.ChargeId, out var chargeId))
            return ServiceResult<PaymentDto>.Failure("ChargeId invalide.", "errors.common.invalid_id");

        if (!ValidMethods.Contains(dto.PaymentMethod))
            return ServiceResult<PaymentDto>.Failure("Méthode de paiement invalide.", "errors.payment.invalid_method");

        var charge = await db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .FirstOrDefaultAsync(c => c.Id == chargeId && c.TenantId == request.TenantId, ct);

        if (charge is null) return ServiceResult<PaymentDto>.Failure("Cotisation introuvable.", "errors.charge.not_found");
        if (charge.Status == "waived") return ServiceResult<PaymentDto>.Failure("Cette cotisation est exonérée.", "errors.charge.waived");
        if (charge.Balance <= 0) return ServiceResult<PaymentDto>.Failure("Cette cotisation est déjà soldée.", "errors.charge.already_paid");
        if (dto.Amount > charge.Balance)
            return ServiceResult<PaymentDto>.Failure($"Le montant ({dto.Amount}) dépasse le solde restant ({charge.Balance}).", "errors.payment.amount_exceeds_balance");

        var sequence = await db.Payments.CountAsync(p => p.TenantId == request.TenantId, ct) + 1;
        var receiptNumber = $"REC-{sequence:D5}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = charge.MemberId,
            ChargeId = chargeId,
            ReceiptNumber = receiptNumber,
            Amount = dto.Amount,
            Currency = "XAF",
            PaymentGateway = dto.PaymentMethod,
            Notes = dto.Notes,
            Status = "pending",
            PaymentDate = DateOnly.Parse(dto.PaymentDate),
        };

        // AmountPaid is updated optimistically on record so the member sees progress.
        // The charge only moves to "paid" when the payment is confirmed by staff.
        charge.AmountPaid += dto.Amount;
        charge.UpdatedAt = DateTime.UtcNow;

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        log.LogInformation("Payment {ReceiptNumber} recorded for charge {ChargeId}", receiptNumber, chargeId);

        var lang = charge.Member.User.PreferredLanguage ?? "fr";
        _ = notif.NotifyAsync(request.TenantId, charge.Member.UserId,
            "payment_recorded",
            lang == "fr" ? "Paiement enregistré" : "Payment recorded",
            lang == "fr"
                ? $"Votre paiement de {dto.Amount} ({charge.ContributionType.Name}) a été enregistré et est en attente de confirmation."
                : $"Your payment of {dto.Amount} ({charge.ContributionType.Name}) has been recorded and is pending confirmation.",
            ct);

        return ServiceResult<PaymentDto>.Success(new PaymentDto(
            payment.Id.ToString(), payment.TenantId.ToString(), payment.MemberId.ToString(),
            $"{charge.Member.User.FirstName} {charge.Member.User.LastName}", charge.Member.MembershipNumber,
            payment.ChargeId.ToString(), charge.ContributionType.Name,
            payment.ReceiptNumber, payment.Amount, payment.Currency,
            dto.PaymentMethod, payment.Notes, payment.ReceiptFileUrl, payment.Status,
            payment.PaymentDate.ToString("yyyy-MM-dd"),
            null, null, null,
            payment.CreatedAt.ToString("O"), null));
    }
}
