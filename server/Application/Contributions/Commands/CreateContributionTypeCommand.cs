using FluentValidation;
using MediatR;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record CreateContributionTypeCommand(Guid TenantId, CreateContributionTypeRequest Dto)
    : IRequest<ServiceResult<ContributionTypeDto>>;

public class CreateContributionTypeValidator : AbstractValidator<CreateContributionTypeCommand>
{
    private static readonly string[] ValidFrequencies = ["monthly", "quarterly", "annual", "one_time"];

    public CreateContributionTypeValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.Frequency).Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Fréquence invalide. Valeurs: monthly, quarterly, annual, one_time");
        RuleFor(x => x.Dto.BaseAmount).GreaterThan(0);
        RuleFor(x => x.Dto.EffectiveFrom).NotEmpty();
    }
}

public class CreateContributionTypeCommandHandler(AppDbContext db, ILogger<CreateContributionTypeCommandHandler> log)
    : IRequestHandler<CreateContributionTypeCommand, ServiceResult<ContributionTypeDto>>
{
    public async Task<ServiceResult<ContributionTypeDto>> Handle(CreateContributionTypeCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        var type = new ContributionType
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = dto.Name,
            Description = dto.Description,
            Frequency = dto.Frequency,
            BaseAmount = dto.BaseAmount,
            LatePenaltyRate = dto.LatePenaltyRate,
            GracePeriodDays = dto.GracePeriodDays,
            IsActive = true,
            EffectiveFrom = DateOnly.Parse(dto.EffectiveFrom),
            EffectiveTo = dto.EffectiveTo is not null ? DateOnly.Parse(dto.EffectiveTo) : null,
        };

        db.ContributionTypes.Add(type);
        await db.SaveChangesAsync(ct);

        log.LogInformation("ContributionType {Name} created for tenant {TenantId}", type.Name, request.TenantId);

        return ServiceResult<ContributionTypeDto>.Success(ToDto(type));
    }

    private static ContributionTypeDto ToDto(ContributionType t) =>
        new(t.Id.ToString(), t.TenantId.ToString(), t.Name, t.Description, t.Frequency,
            t.BaseAmount, t.LatePenaltyRate, t.GracePeriodDays, t.IsActive,
            t.EffectiveFrom.ToString("yyyy-MM-dd"), t.EffectiveTo?.ToString("yyyy-MM-dd"),
            t.CreatedAt.ToString("O"), null);
}
