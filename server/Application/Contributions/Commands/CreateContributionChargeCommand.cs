using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record CreateContributionChargeCommand(Guid TenantId, CreateContributionChargeRequest Dto)
    : IRequest<ServiceResult<ContributionChargeDto>>;

public class CreateContributionChargeValidator : AbstractValidator<CreateContributionChargeCommand>
{
    public CreateContributionChargeValidator()
    {
        RuleFor(x => x.Dto.MemberId).NotEmpty();
        RuleFor(x => x.Dto.ContributionTypeId).NotEmpty();
        RuleFor(x => x.Dto.DueDate).NotEmpty();
    }
}

public class CreateContributionChargeCommandHandler(AppDbContext db, ILogger<CreateContributionChargeCommandHandler> log)
    : IRequestHandler<CreateContributionChargeCommand, ServiceResult<ContributionChargeDto>>
{
    public async Task<ServiceResult<ContributionChargeDto>> Handle(CreateContributionChargeCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (!Guid.TryParse(dto.MemberId, out var memberId))
            return ServiceResult<ContributionChargeDto>.Failure("MemberId invalide.");
        if (!Guid.TryParse(dto.ContributionTypeId, out var typeId))
            return ServiceResult<ContributionChargeDto>.Failure("ContributionTypeId invalide.");

        var member = await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<ContributionChargeDto>.Failure("Membre introuvable.");

        var type = await db.ContributionTypes
            .FirstOrDefaultAsync(t => t.Id == typeId && t.TenantId == request.TenantId, ct);
        if (type is null) return ServiceResult<ContributionChargeDto>.Failure("Plan de cotisation introuvable.");

        var dueDate = DateOnly.Parse(dto.DueDate);
        var amount = dto.AmountOverride ?? type.BaseAmount;

        var charge = new ContributionCharge
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = memberId,
            ContributionTypeId = typeId,
            DueDate = dueDate,
            BaseAmount = amount,
            PenaltyAmount = 0,
            WaiverAmount = 0,
            AmountPaid = 0,
            Status = "pending",
        };

        db.ContributionCharges.Add(charge);
        await db.SaveChangesAsync(ct);

        log.LogInformation("ContributionCharge {Id} created for member {MemberId}", charge.Id, memberId);

        return ServiceResult<ContributionChargeDto>.Success(new ContributionChargeDto(
            charge.Id.ToString(), charge.TenantId.ToString(), charge.MemberId.ToString(),
            $"{member.User.FirstName} {member.User.LastName}", member.MembershipNumber,
            charge.ContributionTypeId.ToString(), type.Name,
            charge.DueDate.ToString("yyyy-MM-dd"),
            charge.BaseAmount, 0, 0, 0, charge.BaseAmount, charge.BaseAmount,
            "pending", true, charge.CreatedAt.ToString("O"), null));
    }
}
