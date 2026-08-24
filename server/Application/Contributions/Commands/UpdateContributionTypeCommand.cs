using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record UpdateContributionTypeCommand(Guid TenantId, Guid Id, UpdateContributionTypeRequest Dto)
    : IRequest<ServiceResult<ContributionTypeDto>>;

public class UpdateContributionTypeCommandHandler(AppDbContext db, ILogger<UpdateContributionTypeCommandHandler> log)
    : IRequestHandler<UpdateContributionTypeCommand, ServiceResult<ContributionTypeDto>>
{
    public async Task<ServiceResult<ContributionTypeDto>> Handle(UpdateContributionTypeCommand request, CancellationToken ct)
    {
        var type = await db.ContributionTypes
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == request.TenantId, ct);

        if (type is null)
            return ServiceResult<ContributionTypeDto>.Failure("Plan de cotisation introuvable.");

        var dto = request.Dto;
        type.Name = dto.Name;
        type.Description = dto.Description;
        type.Frequency = dto.Frequency;
        type.BaseAmount = dto.BaseAmount;
        type.LatePenaltyRate = dto.LatePenaltyRate;
        type.GracePeriodDays = dto.GracePeriodDays;
        type.IsActive = dto.IsActive;
        type.EffectiveFrom = DateOnly.Parse(dto.EffectiveFrom);
        type.EffectiveTo = dto.EffectiveTo is not null ? DateOnly.Parse(dto.EffectiveTo) : null;

        await db.SaveChangesAsync(ct);

        log.LogInformation("ContributionType {Id} updated", request.Id);

        return ServiceResult<ContributionTypeDto>.Success(
            new ContributionTypeDto(type.Id.ToString(), type.TenantId.ToString(),
                type.Name, type.Description, type.Frequency, type.BaseAmount,
                type.LatePenaltyRate, type.GracePeriodDays, type.IsActive,
                type.EffectiveFrom.ToString("yyyy-MM-dd"), type.EffectiveTo?.ToString("yyyy-MM-dd"),
                type.CreatedAt.ToString("O"), type.UpdatedAt?.ToString("O")));
    }
}
