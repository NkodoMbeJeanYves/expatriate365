using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Payments.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

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

public class RecordPaymentCommandHandler(AppDbContext db, ILogger<RecordPaymentCommandHandler> log)
    : IRequestHandler<RecordPaymentCommand, ServiceResult<PaymentDto>>
{
    private static readonly string[] ValidMethods = ["cash", "bank_transfer", "mobile_money", "card", "cheque"];

    public async Task<ServiceResult<PaymentDto>> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (!Guid.TryParse(dto.ChargeId, out var chargeId))
            return ServiceResult<PaymentDto>.Failure("ChargeId invalide.");

        if (!ValidMethods.Contains(dto.PaymentMethod))
            return ServiceResult<PaymentDto>.Failure("Méthode de paiement invalide.");

        var charge = await db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .FirstOrDefaultAsync(c => c.Id == chargeId && c.TenantId == request.TenantId, ct);

        if (charge is null) return ServiceResult<PaymentDto>.Failure("Cotisation introuvable.");
        if (charge.Status == "waived") return ServiceResult<PaymentDto>.Failure("Cette cotisation est exonérée.");
        if (charge.Balance <= 0) return ServiceResult<PaymentDto>.Failure("Cette cotisation est déjà soldée.");
        if (dto.Amount > charge.Balance)
            return ServiceResult<PaymentDto>.Failure($"Le montant ({dto.Amount}) dépasse le solde restant ({charge.Balance}).");

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
