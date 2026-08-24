using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Queries;

public record GetContributionChargeByIdQuery(Guid TenantId, Guid Id)
    : IRequest<ServiceResult<ContributionChargeDto>>;

public class GetContributionChargeByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetContributionChargeByIdQuery, ServiceResult<ContributionChargeDto>>
{
    public async Task<ServiceResult<ContributionChargeDto>> Handle(GetContributionChargeByIdQuery request, CancellationToken ct)
    {
        var c = await db.ContributionCharges
            .Include(x => x.Member).ThenInclude(m => m.User)
            .Include(x => x.ContributionType)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (c is null)
            return ServiceResult<ContributionChargeDto>.Failure("Cotisation introuvable.");

        return ServiceResult<ContributionChargeDto>.Success(new ContributionChargeDto(
            c.Id.ToString(), c.TenantId.ToString(), c.MemberId.ToString(),
            $"{c.Member.User.FirstName} {c.Member.User.LastName}",
            c.Member.MembershipNumber,
            c.ContributionTypeId.ToString(), c.ContributionType.Name,
            c.DueDate.ToString("yyyy-MM-dd"),
            c.BaseAmount, c.PenaltyAmount, c.WaiverAmount, c.AmountPaid,
            c.TotalDue, c.Balance, c.Status, c.IsActive,
            c.CreatedAt.ToString("O"),
            c.UpdatedAt?.ToString("O")));
    }
}
