using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record WaiveChargeCommand(Guid TenantId, Guid ChargeId, WaiveChargeRequest Dto)
    : IRequest<ServiceResult<ContributionChargeDto>>;

public class WaiveChargeCommandHandler(AppDbContext db, ILogger<WaiveChargeCommandHandler> log)
    : IRequestHandler<WaiveChargeCommand, ServiceResult<ContributionChargeDto>>
{
    public async Task<ServiceResult<ContributionChargeDto>> Handle(WaiveChargeCommand request, CancellationToken ct)
    {
        var charge = await db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId && c.TenantId == request.TenantId, ct);

        if (charge is null) return ServiceResult<ContributionChargeDto>.Failure("Cotisation introuvable.");
        if (charge.Status == "paid") return ServiceResult<ContributionChargeDto>.Failure("Cette cotisation est déjà payée.");

        charge.WaiverAmount = request.Dto.WaiverAmount ?? charge.Balance;
        charge.Status = "waived";

        await db.SaveChangesAsync(ct);

        log.LogInformation("Charge {ChargeId} waived", charge.Id);

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
