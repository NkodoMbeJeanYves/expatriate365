using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace server.Application.TransferAssistance.Commands;

public record CreateTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid RequesterMemberId,
    CreateTransferAssistanceRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class CreateTransferAssistanceRequestValidator
    : AbstractValidator<CreateTransferAssistanceRequestCommand>
{
    private static readonly string[] ReceiveMethods = ["bank_transfer", "mobile_money", "orange_money"];

    public CreateTransferAssistanceRequestValidator()
    {
        RuleFor(x => x.RequesterMemberId).NotEmpty();
        RuleFor(x => x.Request.BeneficiaryMemberId)
            .Must(id => id is null || id != Guid.Empty);
        RuleFor(x => x.Request.DestinationCountry).Length(2).Matches("^[A-Za-z]{2}$");
        RuleFor(x => x.Request.AmountMur).GreaterThan(0);
        RuleFor(x => x.Request.ReceiveCurrency).Length(3).Matches("^[A-Za-z]{3}$");
        RuleFor(x => x.Request.ReceiveMethod).Must(ReceiveMethods.Contains);
        RuleFor(x => x.Request.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.BeneficiaryRelationship).MaximumLength(100);
        RuleFor(x => x.Request.BeneficiaryRelationship).NotEmpty()
            .When(x => x.Request.BeneficiaryMemberId is not null);
        RuleFor(x => x.Request.AvailableFrom)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("The availability start date cannot be in the past.");
        RuleFor(x => x.Request.AvailableTo)
            .GreaterThanOrEqualTo(x => x.Request.AvailableFrom);
        RuleFor(x => x.Request.MeetingArea).NotEmpty().MaximumLength(200);
    }
}

public class CreateTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    IValidator<CreateTransferAssistanceRequestCommand> validator,
    IOptions<TransferAssistanceOptions> options,
    ILogger<CreateTransferAssistanceRequestHandler> log)
    : IRequestHandler<CreateTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        CreateTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)),
                "errors.validation.invalid");
        var limits = options.Value;
        if (limits.MaximumRequestAmountMur <= 0 ||
            command.Request.AmountMur < limits.MinimumRequestAmountMur ||
            command.Request.AmountMur > limits.MaximumRequestAmountMur)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The assistance amount limits are not configured or the requested amount is outside them.",
                "errors.transfer_assistance.amount_out_of_range");

        var member = await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m =>
                m.Id == command.RequesterMemberId &&
                m.TenantId == command.TenantId &&
                m.IsActive &&
                m.Status == "active" &&
                m.User.IsActive,
                ct);
        if (member is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active member profile is required to create a request.",
                "errors.transfer_assistance.member_not_eligible");
        var openRequestCount = await db.TransferAssistanceRequests.CountAsync(r =>
            r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.RequesterMemberId &&
            r.IsActive &&
            (r.Status == TransferAssistanceStatuses.Draft ||
             r.Status == TransferAssistanceStatuses.Submitted ||
             r.Status == TransferAssistanceStatuses.Matching ||
             r.Status == TransferAssistanceStatuses.WitnessPending ||
             r.Status == TransferAssistanceStatuses.IntermediaryAssigned ||
             r.Status == TransferAssistanceStatuses.MeetingScheduled ||
             r.Status == TransferAssistanceStatuses.TransferInProgress ||
             r.Status == TransferAssistanceStatuses.TransferSent ||
             r.Status == TransferAssistanceStatuses.Disputed ||
             r.Status == TransferAssistanceStatuses.Suspended),
            ct);
        if (openRequestCount >= 1)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An open transfer assistance request already exists.",
                "errors.transfer_assistance.open_request_exists");

        var request = command.Request;
        if (request.BeneficiaryMemberId == command.RequesterMemberId)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The requester cannot be their own beneficiary.",
                "errors.transfer_assistance.self_beneficiary_forbidden");
        Member? beneficiary = null;
        if (request.BeneficiaryMemberId is { } beneficiaryMemberId)
        {
            beneficiary = await db.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m =>
                    m.Id == beneficiaryMemberId &&
                    m.TenantId == command.TenantId &&
                    m.IsActive &&
                    m.Status == "active" &&
                    m.User.IsActive &&
                    m.User.EmailVerifiedAt != null,
                    ct);
            if (beneficiary is null)
                return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                    "The beneficiary must be an active, email-verified member of this tenant.",
                    "errors.transfer_assistance.beneficiary_not_eligible");
        }

        var entity = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            RequesterMemberId = command.RequesterMemberId,
            OpenRequestKey = $"{command.TenantId:N}:{command.RequesterMemberId:N}",
            BeneficiaryMemberId = beneficiary?.Id,
            BeneficiaryName = beneficiary?.User.FullName ?? string.Empty,
            DestinationCountry = request.DestinationCountry.ToUpperInvariant(),
            AmountMur = request.AmountMur,
            ReceiveCurrency = request.ReceiveCurrency.ToUpperInvariant(),
            ReceiveMethod = request.ReceiveMethod,
            Purpose = request.Purpose.Trim(),
            BeneficiaryRelationship = beneficiary is null
                ? string.Empty
                : (request.BeneficiaryRelationship ?? string.Empty).Trim(),
            AvailableFrom = request.AvailableFrom,
            AvailableTo = request.AvailableTo,
            MeetingArea = request.MeetingArea.Trim(),
            Status = TransferAssistanceStatuses.Draft,
        };

        db.TransferAssistanceRequests.Add(entity);
        audit.Log("transfer_assistance.request.created", member.UserId, command.TenantId,
            "transfer_assistance_request", entity.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, entity, member.UserId, "request_created");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            var concurrentOpenRequest = await db.TransferAssistanceRequests.AsNoTracking().AnyAsync(r =>
                r.OpenRequestKey == $"{command.TenantId:N}:{command.RequesterMemberId:N}", ct);
            if (!concurrentOpenRequest) throw;
            log.LogWarning("Concurrent duplicate open request rejected for member {MemberId}",
                command.RequesterMemberId);
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An open transfer assistance request already exists.",
                "errors.transfer_assistance.open_request_exists");
        }

        log.LogInformation("Transfer assistance request {RequestId} created for member {MemberId}",
            entity.Id, entity.RequesterMemberId);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            CreateTransferAssistanceRequestHandler.ToResponse(entity));
    }

    internal static TransferAssistanceRequestResponse ToResponse(TransferAssistanceRequest entity) => new(
        entity.Id,
        entity.TenantId,
        entity.RequesterMemberId,
        entity.BeneficiaryMemberId,
        entity.BeneficiaryName,
        entity.DestinationCountry,
        entity.AmountMur,
        entity.ReceiveCurrency,
        entity.ReceiveMethod,
        entity.Purpose,
        entity.BeneficiaryRelationship,
        entity.AvailableFrom,
        entity.AvailableTo,
        entity.MeetingArea,
        entity.Status,
        entity.ReviewReason,
        entity.IsActive,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.HelperMemberId,
        entity.WitnessMemberId,
        entity.MeetingAt,
        entity.ExpiresAt,
        entity.RequesterHandoverConfirmed,
        entity.HelperHandoverConfirmed,
        entity.WitnessAttested,
        entity.TransferProvider,
        entity.TransferEvidenceReference,
        entity.TransferSentAt,
        entity.BeneficiaryConfirmation,
        entity.BeneficiaryConfirmedAt,
        entity.DisputeReason,
        entity.BeneficiaryMemberId is null,
        0);
}

public record UpdateTransferAssistanceDraftCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId,
    CreateTransferAssistanceRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class UpdateTransferAssistanceDraftHandler(
    AppDbContext db,
    AuditService audit,
    IValidator<CreateTransferAssistanceRequestCommand> validator,
    IOptions<TransferAssistanceOptions> options,
    ILogger<UpdateTransferAssistanceDraftHandler> log)
    : IRequestHandler<UpdateTransferAssistanceDraftCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        UpdateTransferAssistanceDraftCommand command,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(
            new CreateTransferAssistanceRequestCommand(command.TenantId, command.MemberId, command.Request), ct);
        if (!validation.IsValid)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), "errors.validation.invalid");
        if (options.Value.MaximumRequestAmountMur <= 0 ||
            command.Request.AmountMur < options.Value.MinimumRequestAmountMur ||
            command.Request.AmountMur > options.Value.MaximumRequestAmountMur)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The assistance amount limits are not configured or the requested amount is outside them.",
                "errors.transfer_assistance.amount_out_of_range");
        var entity = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.MemberId && r.Status == TransferAssistanceStatuses.Draft, ct);
        if (entity is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only an owned draft can be edited.", "errors.transfer_assistance.invalid_transition");
        Member? beneficiary = null;
        if (command.Request.BeneficiaryMemberId is { } beneficiaryMemberId)
        {
            beneficiary = await db.Members.Include(m => m.User).FirstOrDefaultAsync(m =>
                m.Id == beneficiaryMemberId && m.Id != command.MemberId &&
                m.TenantId == command.TenantId && m.IsActive && m.Status == "active" &&
                m.User.IsActive && m.User.EmailVerifiedAt != null, ct);
            if (beneficiary is null)
                return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                    "The beneficiary must be an active, email-verified member of this tenant.",
                    "errors.transfer_assistance.beneficiary_not_eligible");
        }

        entity.BeneficiaryMemberId = beneficiary?.Id;
        entity.BeneficiaryName = beneficiary?.User.FullName ?? string.Empty;
        entity.DestinationCountry = command.Request.DestinationCountry.ToUpperInvariant();
        entity.AmountMur = command.Request.AmountMur;
        entity.ReceiveCurrency = command.Request.ReceiveCurrency.ToUpperInvariant();
        entity.ReceiveMethod = command.Request.ReceiveMethod;
        entity.Purpose = command.Request.Purpose.Trim();
        entity.BeneficiaryRelationship = beneficiary is null
            ? string.Empty
            : (command.Request.BeneficiaryRelationship ?? string.Empty).Trim();
        entity.AvailableFrom = command.Request.AvailableFrom;
        entity.AvailableTo = command.Request.AvailableTo;
        entity.MeetingArea = command.Request.MeetingArea.Trim();
        entity.WorkflowVersion++;
        audit.Log("transfer_assistance.request.draft_updated", command.UserId, command.TenantId,
            "transfer_assistance_request", entity.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, entity, command.UserId, "draft_updated");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent draft update rejected for request {RequestId}", entity.Id);
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        log.LogInformation("Draft request {RequestId} updated", entity.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            CreateTransferAssistanceRequestHandler.ToResponse(entity));
    }
}

public record SubmitTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class SubmitTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    IOptions<TransferAssistanceOptions> options,
    ILogger<SubmitTransferAssistanceRequestHandler> log)
    : IRequestHandler<SubmitTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        SubmitTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var memberIsActive = await db.Members.AnyAsync(m =>
            m.Id == command.MemberId &&
            m.TenantId == command.TenantId &&
            m.IsActive &&
            m.Status == "active" &&
            m.User.IsActive,
            ct);
        if (!memberIsActive)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "An active member profile is required to submit a request.",
                "errors.transfer_assistance.member_not_eligible");

        var entity = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.MemberId, ct);
        if (entity is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (entity.Status != TransferAssistanceStatuses.Draft)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only a draft request can be submitted.", "errors.transfer_assistance.invalid_transition");
        if (options.Value.MaximumRequestAmountMur <= 0 ||
            entity.AmountMur < options.Value.MinimumRequestAmountMur ||
            entity.AmountMur > options.Value.MaximumRequestAmountMur)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The assistance amount limits are not configured or the requested amount is outside them.",
                "errors.transfer_assistance.amount_out_of_range");

        entity.Status = TransferAssistanceStatuses.Submitted;
        entity.WorkflowVersion++;
        audit.Log("transfer_assistance.request.submitted", command.UserId, command.TenantId,
            "transfer_assistance_request", entity.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, entity, command.UserId, "request_submitted");
        try
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                log.LogWarning(exception, "Concurrent review rejected for request {RequestId}", entity.Id);
                return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                    "The request changed concurrently. Refresh and try again.",
                    "errors.transfer_assistance.concurrent_update");
            }
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent submit rejected for request {RequestId}", entity.Id);
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        log.LogInformation("Transfer assistance request {RequestId} submitted", entity.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            CreateTransferAssistanceRequestHandler.ToResponse(entity));
    }
}

public record ReviewTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid ReviewerUserId,
    Guid RequestId,
    ReviewTransferAssistanceRequest Request)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class ReviewTransferAssistanceRequestValidator
    : AbstractValidator<ReviewTransferAssistanceRequestCommand>
{
    public ReviewTransferAssistanceRequestValidator()
    {
        RuleFor(x => x.ReviewerUserId).NotEmpty();
        RuleFor(x => x.Request.Decision).Must(d => d is "approve" or "reject" or "suspend");
        RuleFor(x => x.Request.Reason).MaximumLength(1000);
        RuleFor(x => x.Request.Reason)
            .NotEmpty()
            .When(x => x.Request.Decision is "reject" or "suspend");
    }
}

public class ReviewTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    IValidator<ReviewTransferAssistanceRequestCommand> validator,
    IOptions<TransferAssistanceOptions> options,
    ILogger<ReviewTransferAssistanceRequestHandler> log)
    : IRequestHandler<ReviewTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        ReviewTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)),
                "errors.validation.invalid");

        var entity = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId && r.TenantId == command.TenantId, ct);
        if (entity is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (entity.Status != TransferAssistanceStatuses.Submitted)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Only submitted requests can be reviewed.", "errors.transfer_assistance.invalid_transition");
        var isRequester = await db.Members.AnyAsync(
            m => m.Id == entity.RequesterMemberId && m.UserId == command.ReviewerUserId,
            ct);
        if (isRequester)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "A requester cannot review their own request.",
                "errors.transfer_assistance.self_review_forbidden");

        var now = DateTime.UtcNow;
        if (command.Request.Decision == "approve" &&
            entity.AvailableTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc) < now)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request's availability period has already ended.",
                "errors.transfer_assistance.request_expired");

        entity.Status = command.Request.Decision switch
        {
            "approve" => TransferAssistanceStatuses.Matching,
            "reject" => TransferAssistanceStatuses.Rejected,
            "suspend" => TransferAssistanceStatuses.Suspended,
            _ => throw new InvalidOperationException("The review decision was not validated.")
        };
        if (command.Request.Decision == "reject") entity.OpenRequestKey = null;
        if (command.Request.Decision == "approve")
            entity.ExpiresAt = Min(
                entity.AvailableTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                now.AddDays(Math.Clamp(options.Value.MatchingWindowDays, 1, 90)));
        if (command.Request.Decision == "suspend")
            entity.SuspendedFromStatus = TransferAssistanceStatuses.Submitted;
        entity.ReviewedBy = command.ReviewerUserId;
        entity.ReviewedAt = now;
        entity.ReviewReason = string.IsNullOrWhiteSpace(command.Request.Reason)
            ? null
            : command.Request.Reason.Trim();
        entity.WorkflowVersion++;

        var auditAction = command.Request.Decision switch
        {
            "approve" => "transfer_assistance.request.approved",
            "reject" => "transfer_assistance.request.rejected",
            _ => "transfer_assistance.request.suspended"
        };
        audit.Log(auditAction,
            command.ReviewerUserId, command.TenantId, "transfer_assistance_request",
            entity.Id.ToString(), entity.ReviewReason);
        TransferAssistanceWorkflowSupport.AddEvent(db, entity, command.ReviewerUserId,
            $"request_{command.Request.Decision}d", entity.ReviewReason);
        var requesterUserId = await db.Members
            .Where(m => m.Id == entity.RequesterMemberId)
            .Select(m => (Guid?)m.UserId)
            .FirstOrDefaultAsync(ct);
        if (requesterUserId is { } requesterUser)
        {
            var (reviewType, reviewTitle, reviewBody, reviewEvent) = command.Request.Decision switch
            {
                "approve" => (TransferAssistanceNotificationTypes.Review, "Request approved",
                    "Your request was approved and is now visible to potential intermediaries.", "review_approved"),
                "reject" => (TransferAssistanceNotificationTypes.Review, "Request rejected",
                    "Your request was rejected by a reviewer.", "review_rejected"),
                _ => (TransferAssistanceNotificationTypes.CaseUpdate, "Request suspended",
                    "A reviewer suspended your request.", "suspended")
            };
            await TransferAssistanceWorkflowSupport.AddNotificationAsync(db, command.TenantId, requesterUser,
                reviewType, reviewTitle, reviewBody, entity.Id.ToString(), ct, options.Value, reviewEvent);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent review rejected for request {RequestId}", entity.Id);
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        log.LogInformation("Transfer assistance request {RequestId} reviewed with decision {Decision}",
            entity.Id, command.Request.Decision);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            CreateTransferAssistanceRequestHandler.ToResponse(entity));
    }

    private static DateTime Min(DateTime first, DateTime second) => first < second ? first : second;
}

public record CancelTransferAssistanceRequestCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    Guid RequestId)
    : IRequest<ServiceResult<TransferAssistanceRequestResponse>>;

public class CancelTransferAssistanceRequestHandler(
    AppDbContext db,
    AuditService audit,
    ILogger<CancelTransferAssistanceRequestHandler> log)
    : IRequestHandler<CancelTransferAssistanceRequestCommand, ServiceResult<TransferAssistanceRequestResponse>>
{
    public async Task<ServiceResult<TransferAssistanceRequestResponse>> Handle(
        CancelTransferAssistanceRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.TransferAssistanceRequests.FirstOrDefaultAsync(r =>
            r.Id == command.RequestId &&
            r.TenantId == command.TenantId &&
            r.RequesterMemberId == command.MemberId,
            ct);
        if (entity is null)
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "Request not found.", "errors.transfer_assistance.not_found");
        if (entity.Status is not (TransferAssistanceStatuses.Draft or
            TransferAssistanceStatuses.Submitted or TransferAssistanceStatuses.Matching or
            TransferAssistanceStatuses.WitnessPending or TransferAssistanceStatuses.IntermediaryAssigned or
            TransferAssistanceStatuses.MeetingScheduled))
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request can no longer be cancelled in its current state.",
                "errors.transfer_assistance.invalid_transition");

        entity.Status = TransferAssistanceStatuses.Cancelled;
        entity.OpenRequestKey = null;
        entity.WorkflowVersion++;
        var pendingOffers = await db.TransferAssistanceOffers.Where(o => o.RequestId == entity.Id &&
            o.Status == TransferAssistanceOfferStatuses.Pending).ToListAsync(ct);
        foreach (var offer in pendingOffers) offer.Status = TransferAssistanceOfferStatuses.Declined;
        var pendingInvitations = await db.TransferAssistanceWitnessInvitations.Where(i =>
            i.RequestId == entity.Id && i.Status == TransferAssistanceWitnessInvitationStatuses.Pending)
            .ToListAsync(ct);
        foreach (var invitation in pendingInvitations)
        {
            invitation.Status = TransferAssistanceWitnessInvitationStatuses.Declined;
            invitation.RespondedAt = DateTime.UtcNow;
        }
        await TransferAssistanceWorkflowSupport.ClosePendingCandidatesAsync(db, entity.Id, ct);
        audit.Log("transfer_assistance.request.cancelled", command.UserId, command.TenantId,
            "transfer_assistance_request", entity.Id.ToString());
        TransferAssistanceWorkflowSupport.AddEvent(db, entity, command.UserId, "request_cancelled");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent cancellation rejected for request {RequestId}", entity.Id);
            return ServiceResult<TransferAssistanceRequestResponse>.Failure(
                "The request changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        log.LogInformation("Transfer assistance request {RequestId} cancelled", entity.Id);
        return ServiceResult<TransferAssistanceRequestResponse>.Success(
            CreateTransferAssistanceRequestHandler.ToResponse(entity));
    }
}
