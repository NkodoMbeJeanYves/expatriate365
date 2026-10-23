using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.TransferAssistance.Queries;

public record GetMyTransferAssistanceProfileQuery(Guid TenantId, Guid MemberId)
    : IRequest<TransferAssistanceProfileResponse?>;

public class GetMyTransferAssistanceProfileHandler(AppDbContext db)
    : IRequestHandler<GetMyTransferAssistanceProfileQuery, TransferAssistanceProfileResponse?>
{
    public async Task<TransferAssistanceProfileResponse?> Handle(
        GetMyTransferAssistanceProfileQuery query,
        CancellationToken ct)
    {
        var profile = await db.TransferAssistanceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == query.TenantId && p.MemberId == query.MemberId, ct);
        return profile is null
            ? new TransferAssistanceProfileResponse(
                false, TransferAssistanceVerificationStatuses.NotRequested,
                false, TransferAssistanceVerificationStatuses.NotRequested)
            : UpdateTransferAssistanceProfileHandler.ToResponse(profile);
    }
}

public record ListTransferAssistanceProfileReviewQueueQuery(Guid TenantId, int Page, int Limit)
    : IRequest<PagedResult<TransferAssistanceProfileReviewItem>>;

public class ListTransferAssistanceProfileReviewQueueHandler(AppDbContext db)
    : IRequestHandler<ListTransferAssistanceProfileReviewQueueQuery, PagedResult<TransferAssistanceProfileReviewItem>>
{
    public async Task<PagedResult<TransferAssistanceProfileReviewItem>> Handle(
        ListTransferAssistanceProfileReviewQueueQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var profiles = db.TransferAssistanceProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == query.TenantId && p.IsActive &&
                p.Member.IsActive && p.Member.Status == "active" && p.Member.User.IsActive &&
                p.Member.User.Role != "org_admin" &&
                ((p.HelperOptedIn && p.HelperVerificationStatus == TransferAssistanceVerificationStatuses.Pending) ||
                 (p.WitnessOptedIn && p.WitnessVerificationStatus == TransferAssistanceVerificationStatuses.Pending)));
        var total = await profiles.CountAsync(ct);
        var data = await profiles
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new TransferAssistanceProfileReviewItem(
                p.MemberId,
                p.Member.User.FirstName + " " + p.Member.User.LastName,
                p.HelperOptedIn,
                p.HelperVerificationStatus,
                p.WitnessOptedIn,
                p.WitnessVerificationStatus))
            .ToListAsync(ct);
        return PagedResult<TransferAssistanceProfileReviewItem>.Create(data, page, limit, total);
    }
}
