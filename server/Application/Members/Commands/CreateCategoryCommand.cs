using FluentValidation;
using MediatR;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record CreateCategoryCommand(Guid TenantId, CreateCategoryRequest Dto)
    : IRequest<ServiceResult<MembershipCategoryDto>>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.ContributionRate).GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryCommandHandler(AppDbContext db)
    : IRequestHandler<CreateCategoryCommand, ServiceResult<MembershipCategoryDto>>
{
    public async Task<ServiceResult<MembershipCategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var category = new MembershipCategory
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = dto.Name,
            Description = dto.Description,
            ContributionRate = dto.ContributionRate,
            VotingRights = dto.VotingRights,
            WelfareEligible = dto.WelfareEligible,
        };
        db.MembershipCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return ServiceResult<MembershipCategoryDto>.Success(new MembershipCategoryDto(
            category.Id.ToString(), category.Name, category.Description,
            category.ContributionRate, category.VotingRights, category.WelfareEligible, category.IsActive));
    }
}
