using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Members.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Queries;

public record GetMemberCategoriesQuery(Guid TenantId) : IRequest<List<MembershipCategoryDto>>;

public class GetMemberCategoriesQueryHandler(AppDbContext db)
    : IRequestHandler<GetMemberCategoriesQuery, List<MembershipCategoryDto>>
{
    public async Task<List<MembershipCategoryDto>> Handle(GetMemberCategoriesQuery q, CancellationToken ct)
    {
        var raw = await db.MembershipCategories
            .Where(c => c.TenantId == q.TenantId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return raw.Select(c => new MembershipCategoryDto(
            c.Id.ToString(), c.Name, c.Description,
            c.ContributionRate, c.VotingRights, c.WelfareEligible, c.IsActive))
            .ToList();
    }
}
