using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.TransferAssistance.Commands;

internal static class TransferAssistanceWorkflowSupport
{
    public static void AddEvent(
        AppDbContext db,
        TransferAssistanceRequest request,
        Guid? actorUserId,
        string eventType,
        string? details = null)
    {
        db.TransferAssistanceEvents.Add(new TransferAssistanceEvent
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            RequestId = request.Id,
            ActorUserId = actorUserId,
            EventType = eventType,
            Details = details,
        });
    }

    public static async Task AddNotificationAsync(
        AppDbContext db,
        Guid tenantId,
        Guid userId,
        string type,
        string title,
        string body,
        string requestId,
        CancellationToken ct,
        TransferAssistanceOptions? emailOptions = null,
        string? emailEvent = null)
    {
        var disabledTypes = await db.NotificationPreferences.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.UserId == userId && !p.Enabled &&
                        (p.NotificationType == type || p.NotificationType == TransferAssistanceNotificationTypes.EmailChannel))
            .Select(p => p.NotificationType)
            .ToListAsync(ct);
        if (disabledTypes.Contains(type)) return;
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = "transfer_assistance_request",
            EntityId = requestId,
        });

        if (emailOptions is not { EmailNotificationsEnabled: true } || emailEvent is null ||
            disabledTypes.Contains(TransferAssistanceNotificationTypes.EmailChannel))
            return;
        var recipient = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.ContactEmail, u.FirstName, u.LastName, u.PreferredLanguage })
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(recipient?.ContactEmail)) return;
        var bucket = EmailOutbox.ThrottleBucket(DateTime.UtcNow, emailOptions.EmailThrottleMinutes);
        await EmailOutbox.EnqueueAsync(db, tenantId, userId, recipient.ContactEmail,
            $"{recipient.FirstName} {recipient.LastName}".Trim(), recipient.PreferredLanguage,
            EmailOutboxTemplates.TransferAssistanceEvent,
            new Dictionary<string, string> { ["event"] = emailEvent, ["request_id"] = requestId },
            $"ta:{userId:N}:{emailEvent}:{requestId}:{bucket}", ct);
    }

    public static async Task NotifyParticipantsAsync(
        AppDbContext db,
        TransferAssistanceRequest request,
        Guid? excludedUserId,
        string type,
        string title,
        string body,
        TransferAssistanceOptions options,
        string emailEvent,
        CancellationToken ct,
        bool includeBeneficiary = true)
    {
        var memberIds = new List<Guid> { request.RequesterMemberId };
        if (request.HelperMemberId is { } helper) memberIds.Add(helper);
        if (request.WitnessMemberId is { } witness) memberIds.Add(witness);
        if (includeBeneficiary && request.BeneficiaryMemberId is { } beneficiary) memberIds.Add(beneficiary);
        var userIds = await db.Members.AsNoTracking()
            .Where(m => memberIds.Contains(m.Id) && m.TenantId == request.TenantId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);
        foreach (var userId in userIds.Where(id => id != excludedUserId))
            await AddNotificationAsync(db, request.TenantId, userId, type, title, body,
                request.Id.ToString(), ct, options, emailEvent);
    }

    public static async Task<bool> SaveWorkflowAsync(
        AppDbContext db,
        ILogger log,
        Guid requestId,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent transfer assistance update rejected for {RequestId}", requestId);
            return false;
        }
    }

    public static TransferAssistanceRequestResponse ToResponse(TransferAssistanceRequest request) =>
        CreateTransferAssistanceRequestHandler.ToResponse(request);

    public static async Task<int> ClosePendingCandidatesAsync(
        AppDbContext db,
        Guid requestId,
        CancellationToken ct)
    {
        var pending = await db.TransferAssistanceBeneficiaryCandidates
            .Where(c => c.RequestId == requestId &&
                        c.Status == TransferAssistanceBeneficiaryCandidateStatuses.Pending)
            .ToListAsync(ct);
        foreach (var candidate in pending)
            candidate.Status = TransferAssistanceBeneficiaryCandidateStatuses.Declined;
        return pending.Count;
    }

    public static string? ValidateText(string? value, int maximumLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maximumLength ? null : trimmed;
    }
}

public record ListAvailableTransferAssistanceRequestsQuery(Guid TenantId, Guid MemberId)
    : IRequest<IReadOnlyList<TransferAssistanceAvailableRequestResponse>>;

public class ListAvailableTransferAssistanceRequestsHandler(
    AppDbContext db, ILogger<ListAvailableTransferAssistanceRequestsHandler> log,
    IOptions<TransferAssistanceOptions> options)
    : IRequestHandler<ListAvailableTransferAssistanceRequestsQuery, IReadOnlyList<TransferAssistanceAvailableRequestResponse>>
{
    public async Task<IReadOnlyList<TransferAssistanceAvailableRequestResponse>> Handle(
        ListAvailableTransferAssistanceRequestsQuery query,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var profile = await db.TransferAssistanceProfiles.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TenantId == query.TenantId && p.MemberId == query.MemberId && p.IsActive &&
            p.HelperOptedIn && p.HelperVerificationStatus == TransferAssistanceVerificationStatuses.Verified, ct);
        var memberIsEligible = await db.Members.AnyAsync(m =>
            m.Id == query.MemberId && m.TenantId == query.TenantId && m.IsActive &&
            m.Status == "active" && m.User.IsActive && m.User.Role != "org_admin" &&
            (!options.Value.RequireMfa || m.User.MfaEnabled), ct);
        if (profile is null || !memberIsEligible)
        {
            log.LogInformation("Unverified member {MemberId} attempted to browse assistance requests", query.MemberId);
            return [];
        }

        return await db.TransferAssistanceRequests.AsNoTracking()
            .Where(r => r.TenantId == query.TenantId &&
                        r.Status == TransferAssistanceStatuses.Matching &&
                        r.ExpiresAt > now &&
                        r.RequesterMemberId != query.MemberId &&
                        r.BeneficiaryMemberId != query.MemberId &&
                        r.IsActive)
            .OrderBy(r => r.ExpiresAt)
            .Select(r => new TransferAssistanceAvailableRequestResponse(
                r.Id, r.DestinationCountry, r.AmountMur, r.ReceiveCurrency, r.ReceiveMethod,
                r.Purpose, r.BeneficiaryRelationship, r.AvailableFrom, r.AvailableTo,
                r.MeetingArea, r.ExpiresAt!.Value,
                r.Offers.Any(o => o.HelperMemberId == query.MemberId)))
            .Take(100)
            .ToListAsync(ct);
    }
}

public record SubmitTransferAssistanceOfferCommand(
    Guid TenantId,
    Guid HelperMemberId,
    Guid UserId,
    Guid RequestId,
    SubmitTransferAssistanceOfferRequest Request)
    : IRequest<ServiceResult<TransferAssistanceOfferResponse>>;

public class SubmitTransferAssistanceOfferHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<SubmitTransferAssistanceOfferHandler> log)
    : IRequestHandler<SubmitTransferAssistanceOfferCommand, ServiceResult<TransferAssistanceOfferResponse>>
{
    public async Task<ServiceResult<TransferAssistanceOfferResponse>> Handle(
        SubmitTransferAssistanceOfferCommand command,
        CancellationToken ct)
    {
        var availability = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Availability, 200);
        var message = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Message, 500);
        if (availability is null || message is null)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "Availability and offer message are required and must fit the documented limits.",
                "errors.validation.invalid");

        var profile = await db.TransferAssistanceProfiles.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == command.HelperMemberId && p.IsActive &&
            p.HelperOptedIn && p.HelperVerificationStatus == TransferAssistanceVerificationStatuses.Verified, ct);
        if (profile is null)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "A verified, opted-in intermediary profile is required.",
                "errors.transfer_assistance.helper_not_verified");

        var helper = await db.Members.Include(m => m.User).FirstOrDefaultAsync(m =>
            m.Id == command.HelperMemberId && m.TenantId == command.TenantId && m.IsActive &&
            m.Status == "active" && m.User.IsActive && m.User.Role != "org_admin" &&
            (!options.Value.RequireMfa || m.User.MfaEnabled) &&
            m.User.EmailVerifiedAt != null, ct);
        if (helper is null)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "An active member with verified email and the configured authentication requirements is required.",
                "errors.transfer_assistance.member_not_eligible");

        var request = await db.TransferAssistanceRequests
            .Include(r => r.RequesterMember).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId && r.TenantId == command.TenantId &&
                r.Status == TransferAssistanceStatuses.Matching && r.ExpiresAt > DateTime.UtcNow && r.IsActive, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "The request is not accepting offers.",
                "errors.transfer_assistance.request_not_available");
        if (request.RequesterMemberId == command.HelperMemberId || request.BeneficiaryMemberId == command.HelperMemberId)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "Request participants cannot offer to assist their own request.",
                "errors.transfer_assistance.conflict_of_interest");
        var hasPendingCandidacy = await db.TransferAssistanceBeneficiaryCandidates.AnyAsync(c =>
            c.TenantId == command.TenantId && c.RequestId == request.Id &&
            c.MemberId == command.HelperMemberId &&
            c.Status == TransferAssistanceBeneficiaryCandidateStatuses.Pending, ct);
        if (hasPendingCandidacy)
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "A beneficiary candidate cannot also offer to assist the same request.",
                "errors.transfer_assistance.beneficiary_candidate_conflict");

        var existing = await db.TransferAssistanceOffers.FirstOrDefaultAsync(o =>
            o.TenantId == command.TenantId && o.RequestId == request.Id &&
            o.HelperMemberId == command.HelperMemberId, ct);
        if (existing is not null)
        {
            if (existing.Status == TransferAssistanceOfferStatuses.Pending &&
                existing.Availability == availability && existing.Message == message)
                return ServiceResult<TransferAssistanceOfferResponse>.Success(ToResponse(existing, helper.User.FullName));
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "Only one offer per intermediary and request is allowed.",
                "errors.transfer_assistance.offer_already_exists");
        }

        var offer = new TransferAssistanceOffer
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            RequestId = request.Id,
            HelperMemberId = helper.Id,
            Availability = availability,
            Message = message,
        };
        db.TransferAssistanceOffers.Add(offer);
        audit.Log("transfer_assistance.offer.submitted", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "offer_submitted");
        await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
            request.RequesterMember.UserId, TransferAssistanceNotificationTypes.Offer, "New assistance offer",
            "A verified member has offered to help with your request.", request.Id.ToString(), ct,
            options.Value, "offer");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.Entry(offer).State = EntityState.Detached;
            var concurrentOffer = await db.TransferAssistanceOffers.AsNoTracking().FirstOrDefaultAsync(o =>
                o.TenantId == command.TenantId && o.RequestId == request.Id &&
                o.HelperMemberId == helper.Id, ct);
            if (concurrentOffer is null) throw;
            if (concurrentOffer.Status == TransferAssistanceOfferStatuses.Pending &&
                concurrentOffer.Availability == availability && concurrentOffer.Message == message)
                return ServiceResult<TransferAssistanceOfferResponse>.Success(
                    ToResponse(concurrentOffer, helper.User.FullName));
            return ServiceResult<TransferAssistanceOfferResponse>.Failure(
                "Only one offer per intermediary and request is allowed.",
                "errors.transfer_assistance.offer_already_exists");
        }

        log.LogInformation("Member {MemberId} submitted an offer for request {RequestId}",
            helper.Id, request.Id);
        return ServiceResult<TransferAssistanceOfferResponse>.Success(ToResponse(offer, helper.User.FullName));
    }

    internal static TransferAssistanceOfferResponse ToResponse(TransferAssistanceOffer offer, string name) =>
        new(offer.Id, offer.RequestId, offer.HelperMemberId, name, offer.Availability,
            offer.Message, offer.Status, offer.CreatedAt);
}

public record ListMyTransferAssistanceOffersQuery(Guid TenantId, Guid RequesterMemberId, Guid RequestId)
    : IRequest<ServiceResult<IReadOnlyList<TransferAssistanceOfferResponse>>>;

public class ListMyTransferAssistanceOffersHandler(AppDbContext db, ILogger<ListMyTransferAssistanceOffersHandler> log)
    : IRequestHandler<ListMyTransferAssistanceOffersQuery, ServiceResult<IReadOnlyList<TransferAssistanceOfferResponse>>>
{
    public async Task<ServiceResult<IReadOnlyList<TransferAssistanceOfferResponse>>> Handle(
        ListMyTransferAssistanceOffersQuery query,
        CancellationToken ct)
    {
        var ownsRequest = await db.TransferAssistanceRequests.AnyAsync(r => r.Id == query.RequestId &&
            r.TenantId == query.TenantId && r.RequesterMemberId == query.RequesterMemberId, ct);
        if (!ownsRequest)
        {
            log.LogInformation("Member {MemberId} requested offers for a non-owned request {RequestId}",
                query.RequesterMemberId, query.RequestId);
            return ServiceResult<IReadOnlyList<TransferAssistanceOfferResponse>>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        }

        var offers = await db.TransferAssistanceOffers.AsNoTracking()
            .Where(o => o.TenantId == query.TenantId && o.RequestId == query.RequestId)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new TransferAssistanceOfferResponse(
                o.Id, o.RequestId, o.HelperMemberId,
                o.HelperMember.User.FirstName + " " + o.HelperMember.User.LastName,
                o.Availability, o.Message, o.Status, o.CreatedAt))
            .ToListAsync(ct);
        return ServiceResult<IReadOnlyList<TransferAssistanceOfferResponse>>.Success(offers);
    }
}

public record SelectTransferAssistanceOfferCommand(
    Guid TenantId,
    Guid RequesterMemberId,
    Guid UserId,
    Guid RequestId,
    Guid OfferId)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class SelectTransferAssistanceOfferHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<SelectTransferAssistanceOfferHandler> log)
    : IRequestHandler<SelectTransferAssistanceOfferCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        SelectTransferAssistanceOfferCommand command,
        CancellationToken ct)
    {
        var request = await db.TransferAssistanceRequests
            .FirstOrDefaultAsync(r => r.Id == command.RequestId && r.TenantId == command.TenantId &&
                r.RequesterMemberId == command.RequesterMemberId && r.Status == TransferAssistanceStatuses.Matching, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found or it is no longer matching.",
                "errors.transfer_assistance.invalid_transition");
        var offer = await db.TransferAssistanceOffers.Include(o => o.HelperMember).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(o => o.Id == command.OfferId && o.RequestId == request.Id &&
                o.TenantId == command.TenantId && o.Status == TransferAssistanceOfferStatuses.Pending, ct);
        if (offer is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The offer is no longer available.",
                "errors.transfer_assistance.offer_not_found");
        var helperEligible = await db.TransferAssistanceProfiles.AnyAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == offer.HelperMemberId && p.IsActive &&
            p.HelperOptedIn && p.HelperVerificationStatus == TransferAssistanceVerificationStatuses.Verified &&
            p.Member.IsActive && p.Member.Status == "active" && p.Member.User.IsActive &&
            p.Member.User.Role != "org_admin" && p.Member.User.EmailVerifiedAt != null &&
            (!options.Value.RequireMfa || p.Member.User.MfaEnabled), ct);
        if (!helperEligible)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The intermediary is no longer eligible.", "errors.transfer_assistance.helper_not_verified");

        request.HelperMemberId = offer.HelperMemberId;
        request.Status = TransferAssistanceStatuses.IntermediaryAssigned;
        request.ExpiresAt = DateTime.UtcNow.AddDays(Math.Clamp(options.Value.InvitationWindowDays, 1, 30));
        request.WorkflowVersion++;
        offer.Status = TransferAssistanceOfferStatuses.Selected;
        var otherOffers = await db.TransferAssistanceOffers.Where(o => o.RequestId == request.Id &&
            o.Id != offer.Id && o.Status == TransferAssistanceOfferStatuses.Pending).ToListAsync(ct);
        foreach (var other in otherOffers) other.Status = TransferAssistanceOfferStatuses.Declined;
        var declinedHelperIds = otherOffers.Select(o => o.HelperMemberId).ToList();
        var declinedHelperUserIds = await db.Members.AsNoTracking()
            .Where(m => declinedHelperIds.Contains(m.Id))
            .Select(m => m.UserId).ToListAsync(ct);
        foreach (var declinedUserId in declinedHelperUserIds)
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId, declinedUserId,
                TransferAssistanceNotificationTypes.OfferDeclined, "Offer not selected",
                "Another intermediary was selected for a request you offered to assist. Thank you.",
                request.Id.ToString(), ct);

        audit.Log("transfer_assistance.offer.selected", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "intermediary_selected");
        await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
            offer.HelperMember.UserId, TransferAssistanceNotificationTypes.Assignment, "Your intermediary offer was selected",
            "You have been selected as the intermediary for a community transfer assistance request.",
            request.Id.ToString(), ct, options.Value, "assignment");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Request {RequestId} assigned to intermediary {MemberId}", request.Id, offer.HelperMemberId);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record InviteTransferAssistanceWitnessCommand(
    Guid TenantId,
    Guid RequesterMemberId,
    Guid UserId,
    Guid RequestId,
    InviteTransferAssistanceWitnessRequest Request)
    : IRequest<ServiceResult<TransferAssistanceWitnessInvitationResponse>>;

public class InviteTransferAssistanceWitnessHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<InviteTransferAssistanceWitnessHandler> log)
    : IRequestHandler<InviteTransferAssistanceWitnessCommand, ServiceResult<TransferAssistanceWitnessInvitationResponse>>
{
    public async Task<ServiceResult<TransferAssistanceWitnessInvitationResponse>> Handle(
        InviteTransferAssistanceWitnessCommand command,
        CancellationToken ct)
    {
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.RequesterMemberId &&
            r.Status == TransferAssistanceStatuses.WitnessPending, ct);
        if (request is null || request.HelperMemberId is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A selected intermediary is required before inviting a witness.",
                "errors.transfer_assistance.invalid_transition");
        if (request.BeneficiaryMemberId is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A beneficiary must be designated before inviting a witness.",
                "errors.transfer_assistance.beneficiary_required_before_witness");
        var witness = await db.Members.Include(m => m.User).FirstOrDefaultAsync(m =>
            m.Id == command.Request.WitnessMemberId && m.TenantId == command.TenantId && m.IsActive &&
            m.Status == "active" && m.User.IsActive && m.User.Role != "org_admin" &&
            (!options.Value.RequireMfa || m.User.MfaEnabled) &&
            m.User.EmailVerifiedAt != null, ct);
        if (witness is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "The witness must be active with verified email and meet the configured authentication requirements.",
                "errors.transfer_assistance.member_not_eligible");
        if (witness.Id == request.RequesterMemberId || witness.Id == request.HelperMemberId ||
            witness.Id == request.BeneficiaryMemberId)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "The witness must be independent from the requester, intermediary, and beneficiary.",
                "errors.transfer_assistance.conflict_of_interest");
        var profile = await db.TransferAssistanceProfiles.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == witness.Id && p.IsActive &&
            p.WitnessOptedIn && p.WitnessVerificationStatus == TransferAssistanceVerificationStatuses.Verified, ct);
        if (profile is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A verified, opted-in witness profile is required.",
                "errors.transfer_assistance.witness_not_verified");
        if (await db.TransferAssistanceWitnessInvitations.AnyAsync(i =>
                i.TenantId == command.TenantId && i.RequestId == request.Id &&
                i.Status == TransferAssistanceWitnessInvitationStatuses.Pending &&
                i.ExpiresAt > DateTime.UtcNow, ct))
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "Resolve the pending witness invitation before inviting another witness.",
                "errors.transfer_assistance.invitation_pending");

        var invitation = new TransferAssistanceWitnessInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            RequestId = request.Id,
            WitnessMemberId = witness.Id,
            InvitedByUserId = command.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(Math.Clamp(options.Value.InvitationWindowDays, 1, 30)),
        };
        request.ExpiresAt = invitation.ExpiresAt;
        request.WorkflowVersion++;
        db.TransferAssistanceWitnessInvitations.Add(invitation);
        audit.Log("transfer_assistance.witness.invited", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "witness_invited");
        await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
            witness.UserId, TransferAssistanceNotificationTypes.WitnessInvitation, "Witness invitation",
            "You have been invited to witness a community assistance handover.",
            request.Id.ToString(), ct, options.Value, "witness_invitation");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Witness {MemberId} invited to request {RequestId}", witness.Id, request.Id);
        return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Success(
            ToResponse(invitation, witness.User.FullName));
    }

    internal static TransferAssistanceWitnessInvitationResponse ToResponse(
        TransferAssistanceWitnessInvitation invitation,
        string name) =>
        new(invitation.Id, invitation.RequestId, invitation.WitnessMemberId, name,
            invitation.Status, invitation.ConflictDeclared, invitation.ConflictReason, invitation.CreatedAt);
}

public record RespondTransferAssistanceWitnessInvitationCommand(
    Guid TenantId,
    Guid WitnessMemberId,
    Guid UserId,
    Guid InvitationId,
    RespondTransferAssistanceWitnessInvitationRequest Request)
    : IRequest<ServiceResult<TransferAssistanceWitnessInvitationResponse>>;

public class RespondTransferAssistanceWitnessInvitationHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<RespondTransferAssistanceWitnessInvitationHandler> log)
    : IRequestHandler<RespondTransferAssistanceWitnessInvitationCommand, ServiceResult<TransferAssistanceWitnessInvitationResponse>>
{
    public async Task<ServiceResult<TransferAssistanceWitnessInvitationResponse>> Handle(
        RespondTransferAssistanceWitnessInvitationCommand command,
        CancellationToken ct)
    {
        var invitation = await db.TransferAssistanceWitnessInvitations
            .Include(i => i.WitnessMember).ThenInclude(m => m.User)
            .Include(i => i.Request)
            .Include(i => i.Request).ThenInclude(r => r.RequesterMember).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(i => i.Id == command.InvitationId && i.TenantId == command.TenantId &&
                i.WitnessMemberId == command.WitnessMemberId &&
                i.Status == TransferAssistanceWitnessInvitationStatuses.Pending, ct);
        if (invitation is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "Pending witness invitation not found.",
                "errors.transfer_assistance.invitation_not_found");
        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            invitation.Status = TransferAssistanceWitnessInvitationStatuses.Expired;
            invitation.RespondedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "The witness invitation has expired.",
                "errors.transfer_assistance.invitation_expired");
        }
        if (invitation.Request.Status != TransferAssistanceStatuses.WitnessPending ||
            invitation.Request.HelperMemberId is null ||
            invitation.WitnessMember.Status != "active" ||
            !invitation.WitnessMember.IsActive ||
            !invitation.WitnessMember.User.IsActive ||
            (options.Value.RequireMfa && !invitation.WitnessMember.User.MfaEnabled))
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "This invitation can no longer be accepted.",
                "errors.transfer_assistance.invalid_transition");
        if (command.Request.Accept && invitation.WitnessMember.User.Role == "org_admin")
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "Community administrators cannot accept witness assignments.",
                "errors.transfer_assistance.admin_participation_forbidden");
        if (command.Request.ConflictDeclared && command.Request.Accept)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A witness who declares a conflict cannot accept the invitation.",
                "errors.transfer_assistance.conflict_of_interest");
        var conflictReason = TransferAssistanceWorkflowSupport.ValidateText(command.Request.ConflictReason, 500);
        if (command.Request.ConflictDeclared && conflictReason is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A conflict reason is required.",
                "errors.validation.invalid");

        invitation.ConflictDeclared = command.Request.ConflictDeclared;
        invitation.ConflictReason = conflictReason;
        invitation.Status = command.Request.Accept
            ? TransferAssistanceWitnessInvitationStatuses.Accepted
            : TransferAssistanceWitnessInvitationStatuses.Declined;
        invitation.RespondedAt = DateTime.UtcNow;
        var request = invitation.Request;
        var profile = await db.TransferAssistanceProfiles.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == command.WitnessMemberId && p.IsActive &&
            p.WitnessOptedIn && p.WitnessVerificationStatus == TransferAssistanceVerificationStatuses.Verified, ct);
        if (command.Request.Accept && profile is null)
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "A currently verified witness profile is required.",
                "errors.transfer_assistance.witness_not_verified");
        if (command.Request.Accept)
        {
            if (request.WitnessMemberId is not null || request.HelperMemberId == command.WitnessMemberId)
                return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                    "This request can no longer accept a witness.",
                    "errors.transfer_assistance.invalid_transition");
            request.WitnessMemberId = command.WitnessMemberId;
        }
        request.WorkflowVersion++;
        audit.Log(command.Request.Accept
                ? "transfer_assistance.witness.accepted"
                : command.Request.ConflictDeclared
                    ? "transfer_assistance.witness.conflict_declared"
                    : "transfer_assistance.witness.declined",
            command.UserId, command.TenantId, "transfer_assistance_request",
            request.Id.ToString(), conflictReason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId,
            command.Request.Accept ? "witness_accepted" :
            command.Request.ConflictDeclared ? "witness_conflict_declared" : "witness_declined");
        if (command.Request.Accept)
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
                request.RequesterMember.UserId, TransferAssistanceNotificationTypes.WitnessAccepted,
                "Witness accepted", "The invited witness accepted the assignment.",
                request.Id.ToString(), ct, options.Value, "witness_accepted");
        else
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
                request.RequesterMember.UserId, TransferAssistanceNotificationTypes.WitnessDeclined,
                "Witness declined", "The invited witness declined. You can invite another witness.",
                request.Id.ToString(), ct, options.Value, "witness_declined");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Witness invitation {InvitationId} answered with accepted={Accepted}",
            invitation.Id, command.Request.Accept);
        return ServiceResult<TransferAssistanceWitnessInvitationResponse>.Success(
            InviteTransferAssistanceWitnessHandler.ToResponse(invitation, invitation.WitnessMember.User.FullName));
    }
}

public record ScheduleTransferAssistanceMeetingCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId,
    ScheduleTransferAssistanceMeetingRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ScheduleTransferAssistanceMeetingHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ScheduleTransferAssistanceMeetingHandler> log)
    : IRequestHandler<ScheduleTransferAssistanceMeetingCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ScheduleTransferAssistanceMeetingCommand command,
        CancellationToken ct)
    {
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            (r.Status == TransferAssistanceStatuses.IntermediaryAssigned ||
             r.Status == TransferAssistanceStatuses.WitnessPending ||
             r.Status == TransferAssistanceStatuses.MeetingScheduled), ct);
        if (request is null || request.HelperMemberId is null ||
            (request.Status == TransferAssistanceStatuses.WitnessPending && request.WitnessMemberId is null) ||
            (request.RequesterMemberId != command.MemberId && request.HelperMemberId != command.MemberId))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An assigned requester or intermediary is required. Legacy witness-based requests still require an accepted witness.",
                "errors.transfer_assistance.invalid_transition");
        var activeActor = await db.Members.AnyAsync(m => m.Id == command.MemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" && m.User.IsActive, ct);
        if (!activeActor)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active member account is required.", "errors.transfer_assistance.member_not_eligible");
        var meetingAt = command.Request.MeetingAt.Kind == DateTimeKind.Utc
            ? command.Request.MeetingAt
            : command.Request.MeetingAt.ToUniversalTime();
        var latestAvailable = request.AvailableTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        if (meetingAt <= DateTime.UtcNow || meetingAt > latestAvailable)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The meeting must be in the future and within the requested availability period.",
                "errors.validation.invalid");

        request.MeetingAt = meetingAt;
        request.ExpiresAt = meetingAt.AddDays(2);
        request.Status = TransferAssistanceStatuses.MeetingScheduled;
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.meeting.scheduled", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "meeting_scheduled");
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.UserId,
            TransferAssistanceNotificationTypes.Meeting, "Meeting scheduled",
            "A handover meeting has been scheduled.", options.Value, "meeting", ct, includeBeneficiary: false);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Meeting scheduled for request {RequestId}", request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record ConfirmTransferAssistanceHandoverCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId,
    TransferAssistanceHandoverConfirmationRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ConfirmTransferAssistanceHandoverHandler(
    AppDbContext db,
    AuditService audit,
    ILogger<ConfirmTransferAssistanceHandoverHandler> log)
    : IRequestHandler<ConfirmTransferAssistanceHandoverCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ConfirmTransferAssistanceHandoverCommand command,
        CancellationToken ct)
    {
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (request.Status != TransferAssistanceStatuses.MeetingScheduled ||
            request.MeetingAt is null || request.MeetingAt > DateTime.UtcNow ||
            (command.MemberId != request.RequesterMemberId && command.MemberId != request.HelperMemberId))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only the requester or selected intermediary can confirm the scheduled handover.",
                "errors.transfer_assistance.invalid_transition");
        var activeActor = await db.Members.AnyAsync(m => m.Id == command.MemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" && m.User.IsActive, ct);
        if (!activeActor)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active member account is required.", "errors.transfer_assistance.member_not_eligible");
        if (!command.Request.ConfirmPresenceAndHandover)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The confirmation must explicitly attest both presence and handover.",
                "errors.validation.invalid");

        if (command.MemberId == request.RequesterMemberId) request.RequesterHandoverConfirmed = true;
        else request.HelperHandoverConfirmed = true;
        if (request.RequesterHandoverConfirmed && request.HelperHandoverConfirmed &&
            (request.WitnessMemberId is null || request.WitnessAttested))
        {
            request.Status = TransferAssistanceStatuses.TransferInProgress;
            request.HandoverConfirmedAt ??= DateTime.UtcNow;
        }
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.handover.confirmed", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId,
            command.MemberId == request.RequesterMemberId ? "requester_handover_confirmed" : "helper_handover_confirmed");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Handover confirmation recorded for request {RequestId}", request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record AttestTransferAssistanceWitnessCommand(
    Guid TenantId,
    Guid WitnessMemberId,
    Guid UserId,
    Guid RequestId,
    bool Attested)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class AttestTransferAssistanceWitnessHandler(
    AppDbContext db,
    AuditService audit,
    ILogger<AttestTransferAssistanceWitnessHandler> log)
    : IRequestHandler<AttestTransferAssistanceWitnessCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        AttestTransferAssistanceWitnessCommand command,
        CancellationToken ct)
    {
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (!command.Attested || request.Status != TransferAssistanceStatuses.MeetingScheduled ||
            request.WitnessMemberId != command.WitnessMemberId || request.MeetingAt is null ||
            request.MeetingAt > DateTime.UtcNow)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only the accepted witness can attest after the scheduled meeting.",
                "errors.transfer_assistance.invalid_transition");
        var activeWitness = await db.Members.AnyAsync(m => m.Id == command.WitnessMemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" && m.User.IsActive, ct);
        if (!activeWitness)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active witness account is required.", "errors.transfer_assistance.member_not_eligible");

        request.WitnessAttested = true;
        if (request.RequesterHandoverConfirmed && request.HelperHandoverConfirmed)
        {
            request.Status = TransferAssistanceStatuses.TransferInProgress;
            request.HandoverConfirmedAt ??= DateTime.UtcNow;
        }
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.witness.attested", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "witness_handover_attested");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Witness attestation recorded for request {RequestId}", request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record SubmitTransferAssistanceEvidenceCommand(
    Guid TenantId,
    Guid HelperMemberId,
    Guid UserId,
    Guid RequestId,
    SubmitTransferAssistanceEvidenceRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class SubmitTransferAssistanceEvidenceHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<SubmitTransferAssistanceEvidenceHandler> log)
    : IRequestHandler<SubmitTransferAssistanceEvidenceCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        SubmitTransferAssistanceEvidenceCommand command,
        CancellationToken ct)
    {
        var provider = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Provider, 100);
        var reference = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Reference, 120);
        if (provider is null || reference is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A provider name and declared reference are required.",
                "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests
            .Include(r => r.BeneficiaryMember).ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (request.Status != TransferAssistanceStatuses.TransferInProgress ||
            request.HelperMemberId != command.HelperMemberId)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only the assigned intermediary can submit the provider's declared transfer reference at this stage.",
                "errors.transfer_assistance.invalid_transition");
        if (request.BeneficiaryMemberId is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A beneficiary must be designated before the intermediary declares the transfer.",
                "errors.transfer_assistance.beneficiary_required_before_transfer");
        var helperIsActive = await db.Members.AnyAsync(m => m.Id == command.HelperMemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" &&
            m.User.IsActive && m.User.EmailVerifiedAt != null &&
            (!options.Value.RequireMfa || m.User.MfaEnabled), ct);
        if (!helperIsActive)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active, verified intermediary account is required.",
                "errors.transfer_assistance.member_not_eligible");

        request.TransferProvider = provider;
        request.TransferEvidenceReference = reference;
        request.TransferSentAt = DateTime.UtcNow;
        request.Status = TransferAssistanceStatuses.TransferSent;
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.transfer.declared_sent", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "transfer_declared_sent");
        if (request.BeneficiaryMember is not null)
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId,
                request.BeneficiaryMember.UserId,
                TransferAssistanceNotificationTypes.BeneficiaryConfirmation, "Transfer awaiting your confirmation",
                "Please confirm whether you received the expected transfer.",
                request.Id.ToString(), ct, options.Value, "beneficiary_confirmation");
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Declared transfer evidence recorded for request {RequestId}", request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record ConfirmTransferAssistanceReceiptCommand(
    Guid TenantId,
    Guid BeneficiaryMemberId,
    Guid UserId,
    Guid RequestId,
    ConfirmTransferAssistanceReceiptRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ConfirmTransferAssistanceReceiptHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ConfirmTransferAssistanceReceiptHandler> log)
    : IRequestHandler<ConfirmTransferAssistanceReceiptCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ConfirmTransferAssistanceReceiptCommand command,
        CancellationToken ct)
    {
        var outcome = command.Request.Outcome.Trim().ToLowerInvariant();
        var reason = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Reason, 1000);
        if (outcome is not ("full" or "partial" or "not_received") ||
            (outcome != "full" && reason is null))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Choose a valid receipt outcome and provide a reason for a partial or missing receipt.",
                "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.BeneficiaryMemberId == command.BeneficiaryMemberId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (request.Status != TransferAssistanceStatuses.TransferSent ||
            request.BeneficiaryConfirmation is not null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A receipt can only be confirmed once after a transfer declaration.",
                "errors.transfer_assistance.invalid_transition");
        var beneficiaryActive = await db.Members.AnyAsync(m => m.Id == command.BeneficiaryMemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" &&
            m.User.IsActive && m.User.EmailVerifiedAt != null, ct);
        if (!beneficiaryActive)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active, email-verified beneficiary account is required.",
                "errors.transfer_assistance.beneficiary_not_eligible");

        request.BeneficiaryConfirmation = outcome;
        request.BeneficiaryConfirmedAt = DateTime.UtcNow;
        request.Status = outcome == "full"
            ? TransferAssistanceStatuses.Completed
            : TransferAssistanceStatuses.Disputed;
        if (outcome != "full")
        {
            request.DisputeReason = reason;
            request.DisputeOpenedBy = command.UserId;
            request.DisputeOpenedAt = DateTime.UtcNow;
        }
        else
        {
            request.OpenRequestKey = null;
        }
        request.WorkflowVersion++;
        audit.Log($"transfer_assistance.beneficiary.confirmed_{outcome}", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString(), reason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId,
            outcome == "full" ? "beneficiary_confirmed_full" : $"beneficiary_confirmed_{outcome}", reason);
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.UserId,
            TransferAssistanceNotificationTypes.CaseUpdate,
            outcome == "full" ? "Receipt confirmed" : "A dispute was opened",
            outcome == "full"
                ? "The beneficiary confirmed receipt. The case is closed."
                : "The beneficiary reported a partial or missing receipt. A dispute was opened.",
            options.Value, outcome == "full" ? "receipt_confirmed" : "dispute_opened", ct);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Beneficiary confirmation {Outcome} recorded for request {RequestId}",
            outcome, request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record OpenTransferAssistanceDisputeCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId,
    string Reason)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class OpenTransferAssistanceDisputeHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<OpenTransferAssistanceDisputeHandler> log)
    : IRequestHandler<OpenTransferAssistanceDisputeCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        OpenTransferAssistanceDisputeCommand command,
        CancellationToken ct)
    {
        var reason = TransferAssistanceWorkflowSupport.ValidateText(command.Reason, 1000);
        if (reason is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A dispute reason is required.", "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (request.Status is TransferAssistanceStatuses.Completed or TransferAssistanceStatuses.Cancelled or
            TransferAssistanceStatuses.Rejected or TransferAssistanceStatuses.Expired or
            TransferAssistanceStatuses.Disputed or TransferAssistanceStatuses.Suspended ||
            (request.RequesterMemberId != command.MemberId &&
             request.HelperMemberId != command.MemberId &&
             request.WitnessMemberId != command.MemberId &&
             request.BeneficiaryMemberId != command.MemberId))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only an active participant can open a dispute on an undisputed request.",
                "errors.transfer_assistance.invalid_transition");
        var activeParticipant = await db.Members.AnyAsync(m => m.Id == command.MemberId &&
            m.TenantId == command.TenantId && m.IsActive && m.Status == "active" && m.User.IsActive, ct);
        if (!activeParticipant)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active participant account is required.", "errors.transfer_assistance.member_not_eligible");

        request.Status = TransferAssistanceStatuses.Disputed;
        request.DisputeReason = reason;
        request.DisputeOpenedBy = command.UserId;
        request.DisputeOpenedAt = DateTime.UtcNow;
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.dispute.opened", command.UserId, command.TenantId,
            "transfer_assistance_request", request.Id.ToString(), reason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.UserId, "dispute_opened", reason);
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.UserId,
            TransferAssistanceNotificationTypes.CaseUpdate, "A dispute was opened",
            "A participant opened a dispute on this request.", options.Value, "dispute_opened", ct);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogWarning("Dispute opened for transfer assistance request {RequestId}", request.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record ResolveTransferAssistanceDisputeCommand(
    Guid TenantId,
    Guid ReviewerUserId,
    Guid RequestId,
    ResolveTransferAssistanceDisputeRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ResolveTransferAssistanceDisputeHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ResolveTransferAssistanceDisputeHandler> log)
    : IRequestHandler<ResolveTransferAssistanceDisputeCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ResolveTransferAssistanceDisputeCommand command,
        CancellationToken ct)
    {
        var resolution = command.Request.Resolution.Trim().ToLowerInvariant();
        var reason = TransferAssistanceWorkflowSupport.ValidateText(command.Request.Reason, 1000);
        if (resolution is not ("resume" or "cancel") || reason is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Choose resume or cancel and provide a resolution reason.",
                "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.Status == TransferAssistanceStatuses.Disputed, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An open dispute was not found.", "errors.transfer_assistance.dispute_not_found");
        if (resolution == "resume" && request.TransferSentAt is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A dispute without a declared transfer cannot be resumed.",
                "errors.transfer_assistance.invalid_transition");

        request.DisputeResolution = resolution;
        request.DisputeResolvedBy = command.ReviewerUserId;
        request.DisputeResolvedAt = DateTime.UtcNow;
        if (resolution == "resume")
        {
            request.Status = TransferAssistanceStatuses.TransferSent;
            request.BeneficiaryConfirmation = null;
            request.BeneficiaryConfirmedAt = null;
        }
        else
        {
            request.Status = TransferAssistanceStatuses.Cancelled;
            request.OpenRequestKey = null;
        }
        request.WorkflowVersion++;
        audit.Log($"transfer_assistance.dispute.resolved_{resolution}", command.ReviewerUserId,
            command.TenantId, "transfer_assistance_request", request.Id.ToString(), reason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.ReviewerUserId,
            $"dispute_resolved_{resolution}", reason);
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.ReviewerUserId,
            TransferAssistanceNotificationTypes.CaseUpdate, "Dispute resolved",
            "A reviewer resolved the dispute on this request.", options.Value, "dispute_resolved", ct);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Dispute on request {RequestId} resolved with {Resolution}",
            request.Id, resolution);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record SuspendTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid ReviewerUserId,
    Guid RequestId,
    string Reason)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class SuspendTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<SuspendTransferAssistanceRequestHandler> log)
    : IRequestHandler<SuspendTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        SuspendTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var reason = TransferAssistanceWorkflowSupport.ValidateText(command.Reason, 1000);
        if (reason is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A suspension reason is required.", "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (request is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (request.Status is TransferAssistanceStatuses.Completed or TransferAssistanceStatuses.Cancelled or
            TransferAssistanceStatuses.Rejected or TransferAssistanceStatuses.Expired or
            TransferAssistanceStatuses.Disputed or TransferAssistanceStatuses.Suspended)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only an active, undisputed request can be suspended.",
                "errors.transfer_assistance.invalid_transition");

        request.SuspendedFromStatus = request.Status;
        request.Status = TransferAssistanceStatuses.Suspended;
        await TransferAssistanceWorkflowSupport.ClosePendingCandidatesAsync(db, request.Id, ct);
        request.ReviewReason = reason;
        request.ReviewedBy = command.ReviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.request.suspended", command.ReviewerUserId,
            command.TenantId, "transfer_assistance_request", request.Id.ToString(), reason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.ReviewerUserId, "request_suspended", reason);
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.ReviewerUserId,
            TransferAssistanceNotificationTypes.CaseUpdate, "Request suspended",
            "A reviewer suspended this request.", options.Value, "suspended", ct);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogWarning("Request {RequestId} suspended by reviewer {ReviewerId}", request.Id, command.ReviewerUserId);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}

public record ResumeTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid ReviewerUserId,
    Guid RequestId,
    string Reason)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ResumeTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ResumeTransferAssistanceRequestHandler> log)
    : IRequestHandler<ResumeTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ResumeTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var reason = TransferAssistanceWorkflowSupport.ValidateText(command.Reason, 1000);
        if (reason is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A resume reason is required.", "errors.validation.invalid");
        var request = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.Status == TransferAssistanceStatuses.Suspended, ct);
        if (request is null || request.SuspendedFromStatus is null ||
            request.SuspendedFromStatus is TransferAssistanceStatuses.Completed or
                TransferAssistanceStatuses.Cancelled or TransferAssistanceStatuses.Rejected or
                TransferAssistanceStatuses.Expired or TransferAssistanceStatuses.Disputed)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Suspended request cannot be resumed from its recorded state.",
                "errors.transfer_assistance.invalid_transition");

        request.Status = request.SuspendedFromStatus;
        request.SuspendedFromStatus = null;
        request.ReviewReason = reason;
        request.ReviewedBy = command.ReviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.WorkflowVersion++;
        audit.Log("transfer_assistance.request.resumed", command.ReviewerUserId,
            command.TenantId, "transfer_assistance_request", request.Id.ToString(), reason);
        TransferAssistanceWorkflowSupport.AddEvent(db, request, command.ReviewerUserId, "request_resumed", reason);
        await TransferAssistanceWorkflowSupport.NotifyParticipantsAsync(db, request, command.ReviewerUserId,
            TransferAssistanceNotificationTypes.CaseUpdate, "Request resumed",
            "A reviewer resumed this request.", options.Value, "resumed", ct);
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, request.Id, ct))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Request {RequestId} resumed by reviewer {ReviewerId}", request.Id, command.ReviewerUserId);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            TransferAssistanceWorkflowSupport.ToResponse(request));
    }
}
