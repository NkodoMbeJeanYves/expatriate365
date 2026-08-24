using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record MarkChargePaidCommand(Guid TenantId, Guid ChargeId, MarkChargePaidRequest Dto)
    : IRequest<ServiceResult<ContributionChargeDto>>;

public class MarkChargePaidValidator : AbstractValidator<MarkChargePaidCommand>
{
    public MarkChargePaidValidator()
    {
        RuleFor(x => x.Dto.AmountPaid).GreaterThan(0);
    }
}

public class MarkChargePaidCommandHandler(AppDbContext db, ILogger<MarkChargePaidCommandHandler> log)
    : IRequestHandler<MarkChargePaidCommand, ServiceResult<ContributionChargeDto>>
{
    public async Task<ServiceResult<ContributionChargeDto>> Handle(MarkChargePaidCommand request, CancellationToken ct)
    {
        var charge = await db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId && c.TenantId == request.TenantId, ct);

        if (charge is null) return ServiceResult<ContributionChargeDto>.Failure("Cotisation introuvable.");
        if (charge.Status == "waived") return ServiceResult<ContributionChargeDto>.Failure("Cette cotisation est exonérée.");

        charge.AmountPaid += request.Dto.AmountPaid;
        charge.Status = charge.AmountPaid >= charge.TotalDue ? "paid" : "pending";

        await db.SaveChangesAsync(ct);

        log.LogInformation("Charge {ChargeId} marked paid (amount: {Amount})", charge.Id, request.Dto.AmountPaid);

        return ServiceResult<ContributionChargeDto>.Success(new ContributionChargeDto(
            charge.Id.ToString(), charge.TenantId.ToString(), charge.MemberId.ToString(),
            $"{charge.Member.User.FirstName} {charge.Member.User.LastName}", charge.Member.MembershipNumber,
            charge.ContributionTypeId.ToString(), charge.ContributionType.Name,
            charge.DueDate.ToString("yyyy-MM-dd"),
            charge.BaseAmount, charge.PenaltyAmount, charge.WaiverAmount, charge.AmountPaid,
            charge.TotalDue, charge.Balance, charge.Status, charge.IsActive,
            charge.CreatedAt.ToString("O"), charge.UpdatedAt?.ToString("O")));
    }
}
