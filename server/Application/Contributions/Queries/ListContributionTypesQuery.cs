using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Queries;

public record ListContributionTypesQuery(Guid TenantId, bool? IsActive = null)
    : IRequest<List<ContributionTypeDto>>;

public class ListContributionTypesQueryHandler(AppDbContext db)
    : IRequestHandler<ListContributionTypesQuery, List<ContributionTypeDto>>
{
    public async Task<List<ContributionTypeDto>> Handle(ListContributionTypesQuery request, CancellationToken ct)
    {
        var query = db.ContributionTypes
            .Where(t => t.TenantId == request.TenantId);

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        var raw = await query
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return raw.Select(t => new ContributionTypeDto(
                t.Id.ToString(), t.TenantId.ToString(),
                t.Name, t.Description, t.Frequency,
                t.BaseAmount, t.LatePenaltyRate, t.GracePeriodDays,
                t.IsActive,
                t.EffectiveFrom.ToString("yyyy-MM-dd"),
                t.EffectiveTo != null ? t.EffectiveTo.Value.ToString("yyyy-MM-dd") : null,
                t.CreatedAt.ToString("O"), t.UpdatedAt != null ? t.UpdatedAt.Value.ToString("O") : null
            )).ToList();
    }
}
