using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record UpdateCategoryCommand(Guid TenantId, Guid CategoryId, CreateCategoryRequest Dto)
    : IRequest<ServiceResult<MembershipCategoryDto>>;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.ContributionRate).GreaterThanOrEqualTo(0);
    }
}

public class UpdateCategoryCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateCategoryCommand, ServiceResult<MembershipCategoryDto>>
{
    public async Task<ServiceResult<MembershipCategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.MembershipCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == request.TenantId, ct);

        if (cat is null) return ServiceResult<MembershipCategoryDto>.Failure("Catégorie introuvable.");

        cat.Name = request.Dto.Name;
        cat.Description = request.Dto.Description;
        cat.ContributionRate = request.Dto.ContributionRate;
        cat.VotingRights = request.Dto.VotingRights;
        cat.WelfareEligible = request.Dto.WelfareEligible;
        cat.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return ServiceResult<MembershipCategoryDto>.Success(new MembershipCategoryDto(
            cat.Id.ToString(), cat.Name, cat.Description,
            cat.ContributionRate, cat.VotingRights, cat.WelfareEligible, cat.IsActive));
    }
}
