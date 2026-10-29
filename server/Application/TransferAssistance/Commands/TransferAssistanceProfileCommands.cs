using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.TransferAssistance.Commands;

public record UpdateTransferAssistanceProfileCommand(
    Guid TenantId,
    Guid MemberId,
    Guid UserId,
    UpdateTransferAssistanceProfileRequest Request)
    : IRequest<ServiceResult<TransferAssistanceProfileResponse>>;

public class UpdateTransferAssistanceProfileValidator : AbstractValidator<UpdateTransferAssistanceProfileCommand>
{
    public UpdateTransferAssistanceProfileValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.ParticipantRole).Must(r => r is "helper" or "witness")
            .When(x => x.Request.ParticipantRole != null || x.Request.Action != null);
        RuleFor(x => x.Request.Action).Must(a => a is "request" or "cancel")
            .When(x => x.Request.ParticipantRole != null || x.Request.Action != null);
    }
}

public class UpdateTransferAssistanceProfileHandler(
    AppDbContext db,
    AuditService audit,
    IValidator<UpdateTransferAssistanceProfileCommand> validator,
    IOptions<TransferAssistanceOptions> options,
    ILogger<UpdateTransferAssistanceProfileHandler> log)
    : IRequestHandler<UpdateTransferAssistanceProfileCommand, ServiceResult<TransferAssistanceProfileResponse>>
{
    public async Task<ServiceResult<TransferAssistanceProfileResponse>> Handle(
        UpdateTransferAssistanceProfileCommand command,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)),
                "errors.validation.invalid");

        var member = await db.Members.Include(m => m.User).FirstOrDefaultAsync(m =>
            m.Id == command.MemberId &&
            m.TenantId == command.TenantId &&
            m.IsActive &&
            m.Status == "active" &&
            m.User.IsActive,
            ct);
        if (member is null)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "An active member profile is required.",
                "errors.transfer_assistance.member_not_eligible");
        var profile = await db.TransferAssistanceProfiles.FirstOrDefaultAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == command.MemberId, ct);
        if (profile is null)
        {
            profile = new TransferAssistanceProfile
            {
                Id = Guid.NewGuid(),
                TenantId = command.TenantId,
                MemberId = command.MemberId
            };
            db.TransferAssistanceProfiles.Add(profile);
        }

        var helperOptedIn = command.Request.ParticipantRole is null
            ? command.Request.HelperOptedIn
            : command.Request.ParticipantRole == "helper" ? command.Request.Action == "request" : profile.HelperOptedIn;
        var witnessOptedIn = command.Request.ParticipantRole is null
            ? command.Request.WitnessOptedIn
            : command.Request.ParticipantRole == "witness" ? command.Request.Action == "request" : profile.WitnessOptedIn;
        var requesting = command.Request.Action == "request" ||
            (command.Request.ParticipantRole is null &&
             ((!profile.HelperOptedIn && helperOptedIn) || (!profile.WitnessOptedIn && witnessOptedIn)));
        if (member.User.Role == "org_admin" && requesting)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Community administrators cannot be accredited as intermediaries or witnesses.",
                "errors.transfer_assistance.admin_participation_forbidden");
        if (command.Request.Action == "request" &&
            (command.Request.ParticipantRole == "helper" ? profile.HelperOptedIn : profile.WitnessOptedIn))
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Cancel the existing accreditation before requesting another.",
                "errors.transfer_assistance.accreditation_exists");
        if (options.Value.RequireMfa && requesting && !member.User.MfaEnabled)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Multi-factor authentication is required for intermediary and witness roles.",
                "errors.transfer_assistance.mfa_required");
        if (profile.HelperOptedIn == helperOptedIn && profile.WitnessOptedIn == witnessOptedIn)
            return ServiceResult<TransferAssistanceProfileResponse>.Success(ToResponse(profile));

        if (profile.HelperOptedIn != helperOptedIn)
        {
            audit.Log(helperOptedIn ? "transfer_assistance.profile.helper.requested" :
                profile.HelperVerificationStatus == TransferAssistanceVerificationStatuses.Verified
                    ? "transfer_assistance.profile.helper.withdrawn" : "transfer_assistance.profile.helper.cancelled",
                command.UserId, command.TenantId, "transfer_assistance_profile", profile.Id.ToString());
            profile.HelperOptedIn = helperOptedIn;
            profile.HelperVerificationStatus = helperOptedIn
                ? TransferAssistanceVerificationStatuses.Pending
                : TransferAssistanceVerificationStatuses.NotRequested;
            profile.HelperReviewedBy = null;
            profile.HelperReviewedAt = null;
            profile.HelperVerificationReason = null;
        }

        if (profile.WitnessOptedIn != witnessOptedIn)
        {
            audit.Log(witnessOptedIn ? "transfer_assistance.profile.witness.requested" :
                profile.WitnessVerificationStatus == TransferAssistanceVerificationStatuses.Verified
                    ? "transfer_assistance.profile.witness.withdrawn" : "transfer_assistance.profile.witness.cancelled",
                command.UserId, command.TenantId, "transfer_assistance_profile", profile.Id.ToString());
            profile.WitnessOptedIn = witnessOptedIn;
            profile.WitnessVerificationStatus = witnessOptedIn
                ? TransferAssistanceVerificationStatuses.Pending
                : TransferAssistanceVerificationStatuses.NotRequested;
            profile.WitnessReviewedBy = null;
            profile.WitnessReviewedAt = null;
            profile.WitnessVerificationReason = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            log.LogWarning(exception, "Concurrent accreditation update rejected for member {MemberId}", member.Id);
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "The accreditation changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        catch (DbUpdateException exception) when (db.Entry(profile).State == EntityState.Added)
        {
            if (!await db.TransferAssistanceProfiles.AsNoTracking().AnyAsync(p =>
                    p.TenantId == command.TenantId && p.MemberId == command.MemberId, ct)) throw;
            log.LogWarning(exception, "Concurrent accreditation creation rejected for member {MemberId}", member.Id);
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "An accreditation profile already exists. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        }
        log.LogInformation("Transfer assistance profile {ProfileId} updated for member {MemberId}",
            profile.Id, member.Id);
        return ServiceResult<TransferAssistanceProfileResponse>.Success(ToResponse(profile));
    }

    internal static TransferAssistanceProfileResponse ToResponse(TransferAssistanceProfile profile) => new(
        profile.HelperOptedIn,
        profile.HelperVerificationStatus,
        profile.WitnessOptedIn,
        profile.WitnessVerificationStatus);
}

public record ReviewTransferAssistanceProfileCommand(
    Guid TenantId,
    Guid ReviewerUserId,
    Guid MemberId,
    ReviewTransferAssistanceProfileRequest Request)
    : IRequest<ServiceResult<TransferAssistanceProfileResponse>>;

public class ReviewTransferAssistanceProfileValidator : AbstractValidator<ReviewTransferAssistanceProfileCommand>
{
    public ReviewTransferAssistanceProfileValidator()
    {
        RuleFor(x => x.ReviewerUserId).NotEmpty();
        RuleFor(x => x.Request.ParticipantRole).Must(r => r is "helper" or "witness");
        RuleFor(x => x.Request.Decision).Must(d => d is "verify" or "reject");
        RuleFor(x => x.Request.Reason).MaximumLength(1000);
        RuleFor(x => x.Request.Reason).NotEmpty();
    }
}

public class ReviewTransferAssistanceProfileHandler(
    AppDbContext db,
    AuditService audit,
    IValidator<ReviewTransferAssistanceProfileCommand> validator,
    ILogger<ReviewTransferAssistanceProfileHandler> log)
    : IRequestHandler<ReviewTransferAssistanceProfileCommand, ServiceResult<TransferAssistanceProfileResponse>>
{
    public async Task<ServiceResult<TransferAssistanceProfileResponse>> Handle(
        ReviewTransferAssistanceProfileCommand command,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)),
                "errors.validation.invalid");

        var profile = await db.TransferAssistanceProfiles.FirstOrDefaultAsync(p =>
            p.TenantId == command.TenantId && p.MemberId == command.MemberId && p.IsActive, ct);
        if (profile is null)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Participant profile not found.", "errors.transfer_assistance.profile_not_found");
        if (await db.Members.AnyAsync(m => m.Id == profile.MemberId &&
                m.TenantId == command.TenantId && m.User.Role == "org_admin", ct))
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Community administrators cannot be accredited as intermediaries or witnesses.",
                "errors.transfer_assistance.admin_participation_forbidden");
        var isRequester = await db.Members.AnyAsync(
            m => m.Id == profile.MemberId && m.UserId == command.ReviewerUserId,
            ct);
        if (isRequester)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "A participant cannot review their own verification.",
                "errors.transfer_assistance.self_review_forbidden");

        var isHelper = command.Request.ParticipantRole == "helper";
        var optedIn = isHelper ? profile.HelperOptedIn : profile.WitnessOptedIn;
        var status = isHelper ? profile.HelperVerificationStatus : profile.WitnessVerificationStatus;
        if (!optedIn || status != TransferAssistanceVerificationStatuses.Pending)
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "Only an opted-in pending profile can be reviewed.",
                "errors.transfer_assistance.invalid_transition");

        var nextStatus = command.Request.Decision == "verify"
            ? TransferAssistanceVerificationStatuses.Verified
            : TransferAssistanceVerificationStatuses.Rejected;
        if (isHelper)
        {
            profile.HelperVerificationStatus = nextStatus;
            profile.HelperReviewedBy = command.ReviewerUserId;
            profile.HelperReviewedAt = DateTime.UtcNow;
            profile.HelperVerificationReason = command.Request.Reason?.Trim();
        }
        else
        {
            profile.WitnessVerificationStatus = nextStatus;
            profile.WitnessReviewedBy = command.ReviewerUserId;
            profile.WitnessReviewedAt = DateTime.UtcNow;
            profile.WitnessVerificationReason = command.Request.Reason?.Trim();
        }

        var auditAction = command.Request.Decision == "verify"
            ? $"transfer_assistance.profile.{command.Request.ParticipantRole}.verified"
            : $"transfer_assistance.profile.{command.Request.ParticipantRole}.rejected";
        audit.Log(auditAction,
            command.ReviewerUserId, command.TenantId, "transfer_assistance_profile",
            profile.Id.ToString(), command.Request.Reason?.Trim());
        if (!await TransferAssistanceWorkflowSupport.SaveWorkflowAsync(db, log, profile.Id, ct))
            return ServiceResult<TransferAssistanceProfileResponse>.Failure(
                "The accreditation changed concurrently. Refresh and try again.",
                "errors.transfer_assistance.concurrent_update");
        log.LogInformation("Transfer assistance profile {ProfileId} role {ParticipantRole} reviewed as {Decision}",
            profile.Id, command.Request.ParticipantRole, command.Request.Decision);
        return ServiceResult<TransferAssistanceProfileResponse>.Success(
            UpdateTransferAssistanceProfileHandler.ToResponse(profile));
    }
}
