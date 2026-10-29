using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.TransferAssistance.Commands;

internal static class TransferAssistanceBeneficiarySupport
{
    public static IQueryable<Member> EligibleMembers(AppDbContext db, Guid tenantId) =>
        db.Members.Where(m => m.TenantId == tenantId && m.IsActive && m.Status == "active" &&
                              m.User.IsActive && m.User.Role != "org_admin" && m.User.EmailVerifiedAt != null);

    public static IQueryable<TransferAssistanceRequest> OpenRequests(AppDbContext db, Guid tenantId, DateTime now) =>
        db.TransferAssistanceRequests.Where(r => r.TenantId == tenantId && r.IsActive &&
            r.BeneficiaryMemberId == null && r.ExpiresAt > now &&
            (r.Status == TransferAssistanceStatuses.Matching ||
             r.Status == TransferAssistanceStatuses.IntermediaryAssigned ||
             r.Status == TransferAssistanceStatuses.MeetingScheduled ||
             r.Status == TransferAssistanceStatuses.WitnessPending ||
             r.Status == TransferAssistanceStatuses.TransferInProgress));

    public static async Task<bool> HasActiveOfferAsync(AppDbContext db, Guid requestId, Guid memberId, CancellationToken ct) =>
        await db.TransferAssistanceOffers.AnyAsync(o => o.RequestId == requestId && o.HelperMemberId == memberId &&
            (o.Status == TransferAssistanceOfferStatuses.Pending ||
             o.Status == TransferAssistanceOfferStatuses.Selected), ct);
}

public record ListBeneficiaryOpenTransferAssistanceRequestsQuery(Guid TenantId, Guid MemberId)
    : IRequest<IReadOnlyList<TransferAssistanceBeneficiaryOpenRequestResponse>>;

public class ListBeneficiaryOpenTransferAssistanceRequestsHandler(
    AppDbContext db,
    ILogger<ListBeneficiaryOpenTransferAssistanceRequestsHandler> log)
    : IRequestHandler<ListBeneficiaryOpenTransferAssistanceRequestsQuery, IReadOnlyList<TransferAssistanceBeneficiaryOpenRequestResponse>>
{
    public async Task<IReadOnlyList<TransferAssistanceBeneficiaryOpenRequestResponse>> Handle(
        ListBeneficiaryOpenTransferAssistanceRequestsQuery query,
        CancellationToken ct)
    {
        var eligible = await TransferAssistanceBeneficiarySupport.EligibleMembers(db, query.TenantId)
            .AnyAsync(m => m.Id == query.MemberId, ct);
        if (!eligible)
        {
            log.LogInformation("Ineligible member {MemberId} attempted to browse beneficiary-open requests", query.MemberId);
            return [];
        }

        var now = DateTime.UtcNow;
        var requests = await TransferAssistanceBeneficiarySupport.OpenRequests(db, query.TenantId, now)
            .AsNoTracking()
            .Where(r => r.RequesterMemberId != query.MemberId && r.HelperMemberId != query.MemberId)
            .OrderBy(r => r.ExpiresAt)
            .Take(100)
            .Select(r => new
            {
                r.Id, r.DestinationCountry, r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod, r.Purpose,
                r.AvailableFrom, r.AvailableTo, r.ExpiresAt,
            })
            .ToListAsync(ct);
        var requestIds = requests.Select(r => r.Id).ToList();
        var mine = await db.TransferAssistanceBeneficiaryCandidates.AsNoTracking()
            .Where(c => c.MemberId == query.MemberId && requestIds.Contains(c.RequestId))
            .Select(c => new { c.RequestId, c.Id, c.Status })
            .ToListAsync(ct);
        var mineByRequest = mine.ToDictionary(c => c.RequestId);
        log.LogDebug("Member {MemberId} listed {Count} beneficiary-open requests", query.MemberId, requests.Count);
        return requests.Select(r =>
        {
            mineByRequest.TryGetValue(r.Id, out var candidate);
            return new TransferAssistanceBeneficiaryOpenRequestResponse(
                r.Id, r.DestinationCountry, r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod, r.Purpose,
                r.AvailableFrom, r.AvailableTo, r.ExpiresAt, candidate?.Status, candidate?.Id);
        }).ToList();
    }
}

public record ApplyTransferAssistanceBeneficiaryCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId,
    ApplyTransferAssistanceBeneficiaryRequest Request)
    : IRequest<ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>>;

public class ApplyTransferAssistanceBeneficiaryHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ApplyTransferAssistanceBeneficiaryHandler> log)
    : IRequestHandler<ApplyTransferAssistanceBeneficiaryCommand, ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>>
{
    public async Task<ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>> Handle(
        ApplyTransferAssistanceBeneficiaryCommand command,
        CancellationToken ct)
    {
        var message = command.Request.Message?.Trim();
        if (message is { Length: > 300 })
            return Failure("The message must not exceed 300 characters.", "errors.validation.invalid");
        if (string.IsNullOrEmpty(message)) message = null;

        var member = await TransferAssistanceBeneficiarySupport.EligibleMembers(db, command.TenantId)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == command.MemberId, ct);
        if (member is null)
            return Failure("An active member with a verified email is required.",
                "errors.transfer_assistance.member_not_eligible");

        var request = await TransferAssistanceBeneficiarySupport
            .OpenRequests(db, command.TenantId, DateTime.UtcNow)
            .Include(r => r.RequesterMember)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);
        if (request is null)
            return Failure("The request is not looking for a beneficiary.",
                "errors.transfer_assistance.request_not_available");
        if (request.RequesterMemberId == member.Id || request.HelperMemberId == member.Id)
            return Failure("Request participants cannot apply as beneficiary.",
                "errors.transfer_assistance.conflict_of_interest");
        if (await TransferAssistanceBeneficiarySupport.HasActiveOfferAsync(db, request.Id, member.Id, ct))
            return Failure("An intermediary with an active offer cannot apply as beneficiary of the same request.",
                "errors.transfer_assistance.beneficiary_candidate_conflict");

        var candidate = await db.TransferAssistanceBeneficiaryCandidates.FirstOrDefaultAsync(c =>
            c.RequestId == request.Id && c.MemberId == member.Id, ct);
        if (candidate is not null)
        {
            if (candidate.Status == TransferAssistanceBeneficiaryCandidateStatuses.Pending)
                return Success(candidate, member.User.FullName);
            if (candidate.Status != TransferAssistanceBeneficiaryCandidateStatuses.Withdrawn)
                return Failure("A candidacy already exists for this request.",
                    "errors.transfer_assistance.beneficiary_candidate_exists");
            candidate.Status = TransferAssistanceBeneficiaryCandidateStatuses.Pending;
            candidate.Message = message;
            candidate.IsActive = true;
        }
        else
        {
            candidate = new TransferAssistanceBeneficiaryCandidate
            {
                Id = Guid.NewGuid(),
                TenantId = command.TenantId,
                RequestId = request.Id,
                MemberId = member.Id,
                Message = message,
            };
            db.TransferAssistanceBeneficiaryCandidates.Add(candidate);
        }

        audit.Log("transfer_assistance.beneficiary.candidate_submitted", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "beneficiary_candidate_submitted");
        await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
            request.RequesterMember.UserId, TransferAssistanceNotificationTypes.BeneficiaryCandidate,
            "New beneficiary candidate", "A member has applied to become the beneficiary of your request.",
            request.Id.ToString(), ct, options.Value, "beneficiary_candidate");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            log.LogWarning(exception, "Concurrent beneficiary candidacy rejected for request {RequestId}", request.Id);
            return Failure("A candidacy already exists for this request.",
                "errors.transfer_assistance.beneficiary_candidate_exists");
        }

        log.LogInformation("Member {MemberId} applied as beneficiary for request {RequestId}", member.Id, request.Id);
        return Success(candidate, member.User.FullName);
    }

    private static ServiceResult<TransferAssistanceBeneficiaryCandidateResponse> Success(
        TransferAssistanceBeneficiaryCandidate candidate, string name) =>
        ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>.Success(
            TransferAssistanceBeneficiaryCandidateMapper.ToResponse(candidate, name));

    private static ServiceResult<TransferAssistanceBeneficiaryCandidateResponse> Failure(string message, string code) =>
        ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>.Failure(message, code);
}

public record WithdrawTransferAssistanceBeneficiaryCommand(Guid TenantId, Guid MemberId, Guid UserId, Guid RequestId)
    : IRequest<ServiceResult<bool>>;

public class WithdrawTransferAssistanceBeneficiaryHandler(
    AppDbContext db,
    AuditService audit,
    ILogger<WithdrawTransferAssistanceBeneficiaryHandler> log)
    : IRequestHandler<WithdrawTransferAssistanceBeneficiaryCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(WithdrawTransferAssistanceBeneficiaryCommand command, CancellationToken ct)
    {
        var candidate = await db.TransferAssistanceBeneficiaryCandidates.Include(c => c.Request)
            .FirstOrDefaultAsync(c => c.TenantId == command.TenantId && c.RequestId == command.RequestId &&
                                      c.MemberId == command.MemberId, ct);
        if (candidate is null)
            return ServiceResult<bool>.Failure("Candidacy not found.", "errors.transfer_assistance.not_found");
        if (candidate.Status == TransferAssistanceBeneficiaryCandidateStatuses.Withdrawn)
            return ServiceResult<bool>.Success(true);
        if (candidate.Status != TransferAssistanceBeneficiaryCandidateStatuses.Pending)
            return ServiceResult<bool>.Failure("Only a pending candidacy can be withdrawn.",
                "errors.transfer_assistance.invalid_transition");

        candidate.Status = TransferAssistanceBeneficiaryCandidateStatuses.Withdrawn;
        audit.Log("transfer_assistance.beneficiary.candidate_withdrawn", command.UserId, command.TenantId,
            "transfer_assistance_request", command.RequestId.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, candidate.Request, command.UserId, "beneficiary_candidate_withdrawn");
        await db.SaveChangesAsync(ct);
        log.LogInformation("Member {MemberId} withdrew beneficiary candidacy for request {RequestId}",
            command.MemberId, command.RequestId);
        return ServiceResult<bool>.Success(true);
    }
}

public record ListTransferAssistanceBeneficiaryCandidatesQuery(Guid TenantId, Guid RequesterMemberId, Guid RequestId)
    : IRequest<ServiceResult<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>>;

public class ListTransferAssistanceBeneficiaryCandidatesHandler(
    AppDbContext db,
    ILogger<ListTransferAssistanceBeneficiaryCandidatesHandler> log)
    : IRequestHandler<ListTransferAssistanceBeneficiaryCandidatesQuery, ServiceResult<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>>
{
    public async Task<ServiceResult<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>> Handle(
        ListTransferAssistanceBeneficiaryCandidatesQuery query,
        CancellationToken ct)
    {
        var owns = await db.TransferAssistanceRequests.AnyAsync(r => r.Id == query.RequestId &&
            r.TenantId == query.TenantId && r.RequesterMemberId == query.RequesterMemberId, ct);
        if (!owns)
        {
            log.LogInformation("Member {MemberId} requested candidates for a non-owned request {RequestId}",
                query.RequesterMemberId, query.RequestId);
            return ServiceResult<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        }

        var candidates = await db.TransferAssistanceBeneficiaryCandidates.AsNoTracking()
            .Where(c => c.RequestId == query.RequestId && c.TenantId == query.TenantId &&
                        c.Status != TransferAssistanceBeneficiaryCandidateStatuses.Withdrawn)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TransferAssistanceBeneficiaryCandidateResponse(
                c.Id, c.RequestId, c.MemberId, c.Member.User.FirstName + " " + c.Member.User.LastName,
                c.Message, c.Status, c.CreatedAt))
            .ToListAsync(ct);
        log.LogDebug("Listed {Count} beneficiary candidates for request {RequestId}", candidates.Count, query.RequestId);
        return ServiceResult<IReadOnlyList<TransferAssistanceBeneficiaryCandidateResponse>>.Success(candidates);
    }
}

public record DesignateTransferAssistanceBeneficiaryCommand(
    Guid TenantId,
    Guid RequesterMemberId,
    Guid UserId,
    Guid RequestId,
    DesignateTransferAssistanceBeneficiaryRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class DesignateTransferAssistanceBeneficiaryHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<DesignateTransferAssistanceBeneficiaryHandler> log)
    : IRequestHandler<DesignateTransferAssistanceBeneficiaryCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        DesignateTransferAssistanceBeneficiaryCommand command,
        CancellationToken ct)
    {
        var body = command.Request;
        if ((body.CandidateId is null) == (body.MemberId is null))
            return Failure("Provide either a candidate or a member.", "errors.validation.invalid");
        var relationship = body.BeneficiaryRelationship?.Trim() ?? string.Empty;
        if (relationship.Length > 100)
            return Failure("The relationship must not exceed 100 characters.", "errors.validation.invalid");

        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.RequesterMemberId && r.IsActive, ct);
        if (request is null)
            return Failure("Request not found.", "errors.transfer_assistance.not_found");

        TransferAssistanceBeneficiaryCandidate? candidate = null;
        if (body.CandidateId is { } candidateId)
        {
            candidate = await db.TransferAssistanceBeneficiaryCandidates.FirstOrDefaultAsync(c =>
                c.Id == candidateId && c.RequestId == request.Id && c.TenantId == command.TenantId, ct);
            if (candidate is null)
                return Failure("Candidate not found.", "errors.transfer_assistance.not_found");
        }

        var targetMemberId = candidate?.MemberId ?? body.MemberId!.Value;
        if (request.BeneficiaryMemberId is { } current)
        {
            if (current == targetMemberId)
                return ServiceResult<TransferAssistanceRequestResponse>.Success(
                    TransferAssistanceWorkflowSupport.ToResponse(request));
            return Failure("A beneficiary has already been designated.",
                "errors.transfer_assistance.beneficiary_already_designated");
        }

        if (request.Status is not (TransferAssistanceStatuses.Matching or
                TransferAssistanceStatuses.IntermediaryAssigned or
                TransferAssistanceStatuses.MeetingScheduled or
                TransferAssistanceStatuses.WitnessPending or
                TransferAssistanceStatuses.TransferInProgress) ||
            request.ExpiresAt <= DateTime.UtcNow)
            return Failure("A beneficiary can no longer be designated for this request.",
                "errors.transfer_assistance.invalid_transition");
        var witnessInvited = await db.TransferAssistanceWitnessInvitations.AnyAsync(i =>
            i.RequestId == request.Id &&
            (i.Status == TransferAssistanceWitnessInvitationStatuses.Pending ||
             i.Status == TransferAssistanceWitnessInvitationStatuses.Accepted), ct);
        if (request.Status == TransferAssistanceStatuses.WitnessPending && witnessInvited)
            return Failure("A beneficiary cannot be designated after a witness has been invited.",
                "errors.transfer_assistance.invalid_transition");
        if (candidate is not null && candidate.Status != TransferAssistanceBeneficiaryCandidateStatuses.Pending)
            return Failure("The candidacy is no longer pending.", "errors.transfer_assistance.invalid_transition");

        var beneficiary = await TransferAssistanceBeneficiarySupport.EligibleMembers(db, command.TenantId)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == targetMemberId, ct);
        if (beneficiary is null)
            return Failure("The beneficiary must be an active member with a verified email.",
                "errors.transfer_assistance.member_not_eligible");
        if (beneficiary.Id == request.RequesterMemberId || beneficiary.Id == request.HelperMemberId ||
            beneficiary.Id == request.WitnessMemberId ||
            await TransferAssistanceBeneficiarySupport.HasActiveOfferAsync(db, request.Id, beneficiary.Id, ct))
            return Failure("The beneficiary must be independent from the requester, intermediary, and witness.",
                "errors.transfer_assistance.conflict_of_interest");

        request.BeneficiaryMemberId = beneficiary.Id;
        request.BeneficiaryName = beneficiary.User.FullName;
        request.BeneficiaryRelationship = relationship;
        request.WorkflowVersion++;

        var candidates = await db.TransferAssistanceBeneficiaryCandidates
            .Where(c => c.RequestId == request.Id &&
                        c.Status == TransferAssistanceBeneficiaryCandidateStatuses.Pending)
            .ToListAsync(ct);
        var declinedMemberIds = new List<Guid>();
        foreach (var other in candidates)
        {
            if (other.MemberId == beneficiary.Id)
            {
                other.Status = TransferAssistanceBeneficiaryCandidateStatuses.Selected;
                continue;
            }
            other.Status = TransferAssistanceBeneficiaryCandidateStatuses.Declined;
            declinedMemberIds.Add(other.MemberId);
        }

        var declinedUserIds = await db.Members.AsNoTracking()
            .Where(m => declinedMemberIds.Contains(m.Id))
            .Select(m => m.UserId).ToListAsync(ct);
        foreach (var userId in declinedUserIds)
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId, userId,
                TransferAssistanceNotificationTypes.BeneficiaryCandidateDeclined, "Candidacy not selected",
                "Another member was designated as beneficiary for a request you applied to.",
                request.Id.ToString(), ct, options.Value, "beneficiary_candidate_declined");
        await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId, beneficiary.UserId,
            TransferAssistanceNotificationTypes.BeneficiaryDesignated, "You were designated as beneficiary",
            "You have been designated as the beneficiary of a community transfer assistance request.",
            request.Id.ToString(), ct, options.Value, "beneficiary_designated");

        audit.Log("transfer_assistance.beneficiary.designated", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "beneficiary_designated");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return Failure("The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Beneficiary {MemberId} designated for request {RequestId}", beneficiary.Id, request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }

    private static ServiceResult<TransferAssistanceRequestResponse> Failure(string message, string code) =>
        ServiceResult<TransferAssistanceRequestResponse>.Failure(message, code);
}

internal static class TransferAssistanceBeneficiaryCandidateMapper
{
    public static TransferAssistanceBeneficiaryCandidateResponse ToResponse(
        TransferAssistanceBeneficiaryCandidate candidate, string name) =>
        new(candidate.Id, candidate.RequestId, candidate.MemberId, name, candidate.Message,
            candidate.Status, candidate.CreatedAt);
}
