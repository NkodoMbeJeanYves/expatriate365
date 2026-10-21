using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.TransferAssistance.Queries;

public record ListMyTransferAssistanceRequestsQuery(
    Guid TenantId,
    Guid MemberId,
    int Page,
    int Limit)
    : IRequest<PagedResult<TransferAssistanceRequestResponse>>;

public class ListMyTransferAssistanceRequestsHandler(AppDbContext db)
    : IRequestHandler<ListMyTransferAssistanceRequestsQuery, PagedResult<TransferAssistanceRequestResponse>>
{
    public async Task<PagedResult<TransferAssistanceRequestResponse>> Handle(
        ListMyTransferAssistanceRequestsQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var requests = db.TransferAssistanceRequests
            .AsNoTracking()
            .Where(r => r.TenantId == query.TenantId && r.RequesterMemberId == query.MemberId && r.IsActive);
        var total = await requests.CountAsync(ct);
        var data = await requests
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new TransferAssistanceRequestResponse(
                r.Id, r.TenantId, r.RequesterMemberId, r.BeneficiaryMemberId, r.BeneficiaryName, r.DestinationCountry,
                r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod, r.Purpose, r.BeneficiaryRelationship,
                r.AvailableFrom, r.AvailableTo, r.MeetingArea, r.Status, r.ReviewReason, r.IsActive,
                r.CreatedAt, r.UpdatedAt, r.HelperMemberId, r.WitnessMemberId, r.MeetingAt, r.ExpiresAt,
                r.RequesterHandoverConfirmed, r.HelperHandoverConfirmed, r.WitnessAttested,
                r.TransferProvider, r.TransferEvidenceReference, r.TransferSentAt,
                r.BeneficiaryConfirmation, r.BeneficiaryConfirmedAt, r.DisputeReason))
            .ToListAsync(ct);
        return PagedResult<TransferAssistanceRequestResponse>.Create(data, page, limit, total);
    }
}

public record ListTransferAssistanceReviewQueueQuery(
    Guid TenantId,
    int Page,
    int Limit)
    : IRequest<PagedResult<TransferAssistanceRequestResponse>>;

public class ListTransferAssistanceReviewQueueHandler(AppDbContext db)
    : IRequestHandler<ListTransferAssistanceReviewQueueQuery, PagedResult<TransferAssistanceRequestResponse>>
{
    public async Task<PagedResult<TransferAssistanceRequestResponse>> Handle(
        ListTransferAssistanceReviewQueueQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var requests = db.TransferAssistanceRequests
            .AsNoTracking()
            .Where(r => r.TenantId == query.TenantId &&
                        r.Status == TransferAssistanceStatuses.Submitted &&
                        r.IsActive);
        var total = await requests.CountAsync(ct);
        var data = await requests
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new TransferAssistanceRequestResponse(
                r.Id, r.TenantId, r.RequesterMemberId, r.BeneficiaryMemberId, r.BeneficiaryName, r.DestinationCountry,
                r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod, r.Purpose, r.BeneficiaryRelationship,
                r.AvailableFrom, r.AvailableTo, r.MeetingArea, r.Status, r.ReviewReason, r.IsActive,
                r.CreatedAt, r.UpdatedAt, r.HelperMemberId, r.WitnessMemberId, r.MeetingAt, r.ExpiresAt,
                r.RequesterHandoverConfirmed, r.HelperHandoverConfirmed, r.WitnessAttested,
                r.TransferProvider, r.TransferEvidenceReference, r.TransferSentAt,
                r.BeneficiaryConfirmation, r.BeneficiaryConfirmedAt, r.DisputeReason))
            .ToListAsync(ct);
        return PagedResult<TransferAssistanceRequestResponse>.Create(data, page, limit, total);
    }
}

public record GetMyTransferAssistanceRequestQuery(Guid TenantId, Guid MemberId, Guid RequestId)
    : IRequest<TransferAssistanceRequestResponse?>;

public class GetMyTransferAssistanceRequestHandler(AppDbContext db)
    : IRequestHandler<GetMyTransferAssistanceRequestQuery, TransferAssistanceRequestResponse?>
{
    public Task<TransferAssistanceRequestResponse?> Handle(
        GetMyTransferAssistanceRequestQuery query,
        CancellationToken ct) =>
        db.TransferAssistanceRequests
            .AsNoTracking()
            .Where(r => r.Id == query.RequestId &&
                        r.TenantId == query.TenantId &&
                        r.RequesterMemberId == query.MemberId &&
                        r.IsActive)
            .Select(r => new TransferAssistanceRequestResponse(
                r.Id, r.TenantId, r.RequesterMemberId, r.BeneficiaryMemberId, r.BeneficiaryName, r.DestinationCountry,
                r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod, r.Purpose, r.BeneficiaryRelationship,
                r.AvailableFrom, r.AvailableTo, r.MeetingArea, r.Status, r.ReviewReason, r.IsActive,
                r.CreatedAt, r.UpdatedAt, r.HelperMemberId, r.WitnessMemberId, r.MeetingAt, r.ExpiresAt,
                r.RequesterHandoverConfirmed, r.HelperHandoverConfirmed, r.WitnessAttested,
                r.TransferProvider, r.TransferEvidenceReference, r.TransferSentAt,
                r.BeneficiaryConfirmation, r.BeneficiaryConfirmedAt, r.DisputeReason))
            .FirstOrDefaultAsync(ct);
}

public record SearchTransferAssistanceBeneficiariesQuery(Guid TenantId, Guid RequesterMemberId, string Search)
    : IRequest<IReadOnlyList<TransferAssistanceBeneficiaryOption>>;

public class SearchTransferAssistanceBeneficiariesHandler(AppDbContext db)
    : IRequestHandler<SearchTransferAssistanceBeneficiariesQuery, IReadOnlyList<TransferAssistanceBeneficiaryOption>>
{
    public async Task<IReadOnlyList<TransferAssistanceBeneficiaryOption>> Handle(
        SearchTransferAssistanceBeneficiariesQuery query,
        CancellationToken ct)
    {
        var search = query.Search.Trim();
        if (search.Length < 3) return [];

        return await db.Members
            .AsNoTracking()
            .Where(m => m.TenantId == query.TenantId &&
                        m.Id != query.RequesterMemberId &&
                        m.IsActive &&
                        m.Status == "active" &&
                        m.User.IsActive &&
                        m.User.EmailVerifiedAt != null &&
                        (m.User.FirstName.Contains(search) || m.User.LastName.Contains(search)))
            .OrderBy(m => m.User.LastName)
            .ThenBy(m => m.User.FirstName)
            .Take(20)
            .Select(m => new TransferAssistanceBeneficiaryOption(
                m.Id, m.User.FirstName + " " + m.User.LastName))
            .ToListAsync(ct);
    }
}

public record SearchTransferAssistanceWitnessesQuery(Guid TenantId, Guid SearcherMemberId, string Search)
    : IRequest<IReadOnlyList<TransferAssistanceBeneficiaryOption>>;

public class SearchTransferAssistanceWitnessesHandler(
    AppDbContext db, IOptions<TransferAssistanceOptions> options,
    ILogger<SearchTransferAssistanceWitnessesHandler> log)
    : IRequestHandler<SearchTransferAssistanceWitnessesQuery, IReadOnlyList<TransferAssistanceBeneficiaryOption>>
{
    public async Task<IReadOnlyList<TransferAssistanceBeneficiaryOption>> Handle(
        SearchTransferAssistanceWitnessesQuery query,
        CancellationToken ct)
    {
        var search = query.Search.Trim();
        log.LogInformation("Searching eligible witnesses for member {MemberId} in tenant {TenantId}",
            query.SearcherMemberId, query.TenantId);
        if (search.Length < 3) return [];
        return await db.TransferAssistanceProfiles.AsNoTracking()
            .Where(p => p.TenantId == query.TenantId && p.MemberId != query.SearcherMemberId &&
                        p.IsActive && p.WitnessOptedIn &&
                        p.WitnessVerificationStatus == TransferAssistanceVerificationStatuses.Verified &&
                        p.Member.IsActive && p.Member.Status == "active" &&
                        p.Member.User.IsActive && p.Member.User.Role != "org_admin" &&
                        (!options.Value.RequireMfa || p.Member.User.MfaEnabled) &&
                        (p.Member.User.FirstName.Contains(search) || p.Member.User.LastName.Contains(search)))
            .OrderBy(p => p.Member.User.LastName)
            .ThenBy(p => p.Member.User.FirstName)
            .Take(20)
            .Select(p => new TransferAssistanceBeneficiaryOption(
                p.MemberId, p.Member.User.FirstName + " " + p.Member.User.LastName))
            .ToListAsync(ct);
    }
}

public record ListMyTransferAssistanceAssignmentsQuery(Guid TenantId, Guid MemberId)
    : IRequest<IReadOnlyList<TransferAssistanceAssignmentResponse>>;

public class ListMyTransferAssistanceAssignmentsHandler(
    AppDbContext db,
    ILogger<ListMyTransferAssistanceAssignmentsHandler> log)
    : IRequestHandler<ListMyTransferAssistanceAssignmentsQuery, IReadOnlyList<TransferAssistanceAssignmentResponse>>
{
    public async Task<IReadOnlyList<TransferAssistanceAssignmentResponse>> Handle(
        ListMyTransferAssistanceAssignmentsQuery query,
        CancellationToken ct)
    {
        var helperAssignments = await db.TransferAssistanceRequests.AsNoTracking()
            .Where(r => r.TenantId == query.TenantId && r.HelperMemberId == query.MemberId && r.IsActive &&
                        r.Status != TransferAssistanceStatuses.Cancelled &&
                        r.Status != TransferAssistanceStatuses.Rejected &&
                        r.Status != TransferAssistanceStatuses.Expired)
            .Select(r => new TransferAssistanceAssignmentResponse(
                r.Id, "helper", r.Status, r.DestinationCountry, r.AmountMur, r.ReceiveCurrency,
                r.ReceiveMethod, r.Purpose, r.AvailableFrom, r.AvailableTo, r.MeetingArea,
                r.HelperMemberId, r.WitnessMemberId, r.MeetingAt, null,
                r.RequesterHandoverConfirmed, r.HelperHandoverConfirmed, r.WitnessAttested,
                r.TransferProvider, r.TransferEvidenceReference, r.TransferSentAt,
                r.BeneficiaryConfirmation, r.DisputeReason))
            .ToListAsync(ct);
        var witnessAssignments = await db.TransferAssistanceWitnessInvitations.AsNoTracking()
            .Where(i => i.TenantId == query.TenantId && i.WitnessMemberId == query.MemberId &&
                        (i.Status == TransferAssistanceWitnessInvitationStatuses.Accepted ||
                         (i.Status == TransferAssistanceWitnessInvitationStatuses.Pending &&
                          i.ExpiresAt > DateTime.UtcNow)) &&
                        i.Request.IsActive &&
                        i.Request.Status != TransferAssistanceStatuses.Cancelled &&
                        i.Request.Status != TransferAssistanceStatuses.Rejected &&
                        i.Request.Status != TransferAssistanceStatuses.Expired)
            .Select(i => new TransferAssistanceAssignmentResponse(
                i.RequestId, "witness", i.Request.Status, i.Request.DestinationCountry,
                i.Request.AmountMur, i.Request.ReceiveCurrency, i.Request.ReceiveMethod,
                i.Request.Purpose, i.Request.AvailableFrom, i.Request.AvailableTo,
                i.Request.MeetingArea, i.Request.HelperMemberId, i.Request.WitnessMemberId,
                i.Request.MeetingAt, i.Status, i.Request.RequesterHandoverConfirmed,
                i.Request.HelperHandoverConfirmed, i.Request.WitnessAttested,
                i.Request.TransferProvider, i.Request.TransferEvidenceReference,
                i.Request.TransferSentAt, i.Request.BeneficiaryConfirmation, i.Request.DisputeReason))
            .ToListAsync(ct);
        var beneficiaryAssignments = await db.TransferAssistanceRequests.AsNoTracking()
            .Where(r => r.TenantId == query.TenantId && r.BeneficiaryMemberId == query.MemberId &&
                        (r.Status == TransferAssistanceStatuses.TransferSent ||
                         r.Status == TransferAssistanceStatuses.Disputed ||
                         r.Status == TransferAssistanceStatuses.Completed))
            .Select(r => new TransferAssistanceAssignmentResponse(
                r.Id, "beneficiary", r.Status, r.DestinationCountry, r.AmountMur,
                r.ReceiveCurrency, r.ReceiveMethod, r.Purpose, r.AvailableFrom, r.AvailableTo,
                r.MeetingArea, r.HelperMemberId, r.WitnessMemberId, r.MeetingAt, null,
                r.RequesterHandoverConfirmed, r.HelperHandoverConfirmed, r.WitnessAttested,
                r.TransferProvider, r.TransferEvidenceReference, r.TransferSentAt,
                r.BeneficiaryConfirmation, r.DisputeReason))
            .ToListAsync(ct);

        log.LogInformation("Loaded {HelperCount} helper, {WitnessCount} witness, and {BeneficiaryCount} beneficiary assignments for member {MemberId}",
            helperAssignments.Count, witnessAssignments.Count, beneficiaryAssignments.Count, query.MemberId);
        return helperAssignments.Concat(witnessAssignments).Concat(beneficiaryAssignments)
            .OrderByDescending(a => a.MeetingAt ?? DateTime.MinValue)
            .ToList();
    }
}

public record TransferAssistanceEventQuery(
    Guid TenantId,
    Guid MemberId,
    Guid RequestId,
    bool CanReview)
    : IRequest<IReadOnlyList<TransferAssistanceEventResponse>?>;

public class TransferAssistanceEventQueryHandler(AppDbContext db, ILogger<TransferAssistanceEventQueryHandler> log)
    : IRequestHandler<TransferAssistanceEventQuery, IReadOnlyList<TransferAssistanceEventResponse>?>
{
    public async Task<IReadOnlyList<TransferAssistanceEventResponse>?> Handle(
        TransferAssistanceEventQuery query,
        CancellationToken ct)
    {
        var allowed = query.CanReview || await db.TransferAssistanceRequests.AnyAsync(r =>
            r.Id == query.RequestId && r.TenantId == query.TenantId &&
            (r.RequesterMemberId == query.MemberId ||
             r.HelperMemberId == query.MemberId ||
             r.WitnessMemberId == query.MemberId ||
             r.BeneficiaryMemberId == query.MemberId), ct);
        if (!allowed)
        {
            log.LogInformation("Member {MemberId} attempted unauthorized event access for request {RequestId}",
                query.MemberId, query.RequestId);
            return null;
        }

        return await db.TransferAssistanceEvents.AsNoTracking()
            .Where(e => e.TenantId == query.TenantId && e.RequestId == query.RequestId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new TransferAssistanceEventResponse(e.EventType, e.Details, e.CreatedAt))
            .ToListAsync(ct);
    }
}

public record ListMyTransferAssistanceWitnessInvitationsQuery(Guid TenantId, Guid MemberId)
    : IRequest<IReadOnlyList<TransferAssistanceMyInvitationResponse>>;

public class ListMyTransferAssistanceWitnessInvitationsHandler(
    AppDbContext db,
    ILogger<ListMyTransferAssistanceWitnessInvitationsHandler> log)
    : IRequestHandler<ListMyTransferAssistanceWitnessInvitationsQuery, IReadOnlyList<TransferAssistanceMyInvitationResponse>>
{
    public async Task<IReadOnlyList<TransferAssistanceMyInvitationResponse>> Handle(
        ListMyTransferAssistanceWitnessInvitationsQuery query,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var invitations = await db.TransferAssistanceWitnessInvitations.AsNoTracking()
            .Where(i => i.TenantId == query.TenantId && i.WitnessMemberId == query.MemberId &&
                        i.Status == TransferAssistanceWitnessInvitationStatuses.Pending &&
                        i.ExpiresAt > now && i.Request.Status == TransferAssistanceStatuses.WitnessPending)
            .OrderBy(i => i.ExpiresAt)
            .Select(i => new TransferAssistanceMyInvitationResponse(
                i.Id, i.RequestId, i.Request.DestinationCountry, i.Request.AmountMur,
                i.Request.ReceiveCurrency, i.ExpiresAt))
            .ToListAsync(ct);
        log.LogInformation("Loaded {InvitationCount} pending witness invitations for member {MemberId}",
            invitations.Count, query.MemberId);
        return invitations;
    }
}

public record ListTransferAssistanceReviewCasesQuery(
    Guid TenantId,
    string Status,
    int Page,
    int Limit)
    : IRequest<PagedResult<TransferAssistanceReviewCaseResponse>>;

public class ListTransferAssistanceReviewCasesHandler(
    AppDbContext db,
    ILogger<ListTransferAssistanceReviewCasesHandler> log)
    : IRequestHandler<ListTransferAssistanceReviewCasesQuery, PagedResult<TransferAssistanceReviewCaseResponse>>
{
    public async Task<PagedResult<TransferAssistanceReviewCaseResponse>> Handle(
        ListTransferAssistanceReviewCasesQuery query,
        CancellationToken ct)
    {
        if (query.Status is not (TransferAssistanceStatuses.Disputed or TransferAssistanceStatuses.Suspended))
            throw new ArgumentOutOfRangeException(nameof(query.Status), "Unsupported review case status.");
        var page = Math.Max(1, query.Page);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var cases = db.TransferAssistanceRequests.AsNoTracking().Where(r =>
            r.TenantId == query.TenantId && r.Status == query.Status && r.IsActive);
        var total = await cases.CountAsync(ct);
        var data = await cases.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Skip((page - 1) * limit).Take(limit)
            .Select(r => new TransferAssistanceReviewCaseResponse(
                r.Id, r.Status, r.DestinationCountry, r.AmountMur, r.ReceiveCurrency,
                r.BeneficiaryName, r.ReviewReason, r.DisputeReason, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(ct);
        log.LogInformation("Loaded {CaseCount} {Status} review cases for tenant {TenantId}",
            data.Count, query.Status, query.TenantId);
        return PagedResult<TransferAssistanceReviewCaseResponse>.Create(data, page, limit, total);
    }
}
