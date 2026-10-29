using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Application.TransferAssistance.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Tests;

public class TransferAssistanceMfaTests
{
    [Fact]
    public void MissingConfiguration_RequiresMfa() =>
        Assert.True(new TransferAssistanceOptions().RequireMfa);

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task MfaPolicy_IsAppliedAcrossHelperAndWitnessWorkflow(
        bool requireMfa, bool mfaEnabled, bool allowed)
    {
        await using var db = TestDbFactory.Create();
        var (request, helper, witness) = await SeedAsync(db, mfaEnabled);
        var options = Options.Create(new TransferAssistanceOptions { RequireMfa = requireMfa });
        var audit = new AuditService(db);
        var profileHandler = new UpdateTransferAssistanceProfileHandler(
            db, audit, new UpdateTransferAssistanceProfileValidator(), options,
            NullLogger<UpdateTransferAssistanceProfileHandler>.Instance);
        foreach (var participant in new[] { helper, witness })
        {
            var signup = await profileHandler.Handle(new UpdateTransferAssistanceProfileCommand(
                request.TenantId, participant.Id, participant.UserId,
                new UpdateTransferAssistanceProfileRequest(true, true)), default);
            Assert.Equal(allowed, signup.IsSuccess);
            if (!allowed) Assert.Equal("errors.transfer_assistance.mfa_required", signup.ErrorCode);
        }

        // Seed verified profiles so every downstream guard is tested independently of signup.
        foreach (var participant in new[] { helper, witness })
        {
            var profile = db.TransferAssistanceProfiles.Local.SingleOrDefault(p => p.MemberId == participant.Id);
            if (profile is null)
            {
                profile = new TransferAssistanceProfile
                {
                    Id = Guid.NewGuid(), TenantId = request.TenantId, MemberId = participant.Id,
                };
                db.TransferAssistanceProfiles.Add(profile);
            }
            profile.HelperOptedIn = profile.WitnessOptedIn = true;
            profile.HelperVerificationStatus = profile.WitnessVerificationStatus =
                TransferAssistanceVerificationStatuses.Verified;
        }
        await db.SaveChangesAsync();

        var available = await new ListAvailableTransferAssistanceRequestsHandler(
            db, NullLogger<ListAvailableTransferAssistanceRequestsHandler>.Instance, options)
            .Handle(new ListAvailableTransferAssistanceRequestsQuery(request.TenantId, helper.Id), default);
        Assert.Equal(allowed, available.Any(r => r.Id == request.Id));

        var offer = await new SubmitTransferAssistanceOfferHandler(
            db, audit, options, NullLogger<SubmitTransferAssistanceOfferHandler>.Instance)
            .Handle(new SubmitTransferAssistanceOfferCommand(
                request.TenantId, helper.Id, helper.UserId, request.Id,
                new SubmitTransferAssistanceOfferRequest("Tomorrow", "I can help.")), default);
        Assert.Equal(allowed, offer.IsSuccess);
        if (!allowed) Assert.Equal("errors.transfer_assistance.member_not_eligible", offer.ErrorCode);

        var witnesses = await new SearchTransferAssistanceWitnessesHandler(
            db, options, NullLogger<SearchTransferAssistanceWitnessesHandler>.Instance)
            .Handle(new SearchTransferAssistanceWitnessesQuery(request.TenantId, helper.Id, "Witness"), default);
        Assert.Equal(allowed, witnesses.Any(w => w.MemberId == witness.Id));

        request.HelperMemberId = helper.Id;
        request.Status = TransferAssistanceStatuses.WitnessPending;
        await db.SaveChangesAsync();
        var invitation = await new InviteTransferAssistanceWitnessHandler(
            db, audit, options, NullLogger<InviteTransferAssistanceWitnessHandler>.Instance)
            .Handle(new InviteTransferAssistanceWitnessCommand(
                request.TenantId, request.RequesterMemberId, request.RequesterMember.UserId, request.Id,
                new InviteTransferAssistanceWitnessRequest(witness.Id)), default);
        Assert.Equal(allowed, invitation.IsSuccess);
        if (!allowed) Assert.Equal("errors.transfer_assistance.member_not_eligible", invitation.ErrorCode);

        var pending = db.TransferAssistanceWitnessInvitations.Local.SingleOrDefault();
        if (pending is null)
        {
            pending = new TransferAssistanceWitnessInvitation
            {
                Id = Guid.NewGuid(), TenantId = request.TenantId, RequestId = request.Id,
                WitnessMemberId = witness.Id, InvitedByUserId = request.RequesterMember.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
            };
            db.TransferAssistanceWitnessInvitations.Add(pending);
            await db.SaveChangesAsync();
        }
        var response = await new RespondTransferAssistanceWitnessInvitationHandler(
            db, audit, options, NullLogger<RespondTransferAssistanceWitnessInvitationHandler>.Instance)
            .Handle(new RespondTransferAssistanceWitnessInvitationCommand(
                request.TenantId, witness.Id, witness.UserId, pending.Id,
                new RespondTransferAssistanceWitnessInvitationRequest(true, false, null)), default);
        Assert.Equal(allowed, response.IsSuccess);
        if (!allowed) Assert.Equal("errors.transfer_assistance.invalid_transition", response.ErrorCode);

        request.Status = TransferAssistanceStatuses.TransferInProgress;
        await db.SaveChangesAsync();
        var evidence = await new SubmitTransferAssistanceEvidenceHandler(
            db, audit, options, NullLogger<SubmitTransferAssistanceEvidenceHandler>.Instance)
            .Handle(new SubmitTransferAssistanceEvidenceCommand(
                request.TenantId, helper.Id, helper.UserId, request.Id,
                new SubmitTransferAssistanceEvidenceRequest("External provider", "TEST-REF")), default);
        Assert.Equal(allowed, evidence.IsSuccess);
        Assert.Equal(allowed ? TransferAssistanceStatuses.TransferSent : TransferAssistanceStatuses.TransferInProgress,
            request.Status);
        Assert.Equal(mfaEnabled, helper.User.MfaEnabled);
        Assert.Equal(mfaEnabled, witness.User.MfaEnabled);
    }

    [Theory]
    [InlineData("inactive_member")]
    [InlineData("suspended_member")]
    [InlineData("inactive_user")]
    [InlineData("unverified_email")]
    [InlineData("not_opted_in")]
    [InlineData("not_verified")]
    [InlineData("other_tenant")]
    [InlineData("own_request")]
    [InlineData("beneficiary")]
    [InlineData("org_admin")]
    public async Task OptionalMfa_DoesNotBypassOfferEligibility(string condition)
    {
        await using var db = TestDbFactory.Create();
        var (request, helper, _) = await SeedAsync(db, false);
        var profile = new TransferAssistanceProfile
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId, MemberId = helper.Id,
            HelperOptedIn = true, HelperVerificationStatus = TransferAssistanceVerificationStatuses.Verified,
        };
        db.TransferAssistanceProfiles.Add(profile);
        switch (condition)
        {
            case "inactive_member": helper.IsActive = false; break;
            case "suspended_member": helper.Status = "suspended"; break;
            case "inactive_user": helper.User.IsActive = false; break;
            case "unverified_email": helper.User.EmailVerifiedAt = null; break;
            case "not_opted_in": profile.HelperOptedIn = false; break;
            case "not_verified": profile.HelperVerificationStatus = TransferAssistanceVerificationStatuses.Pending; break;
            case "other_tenant": helper.TenantId = Guid.NewGuid(); break;
            case "own_request": request.RequesterMemberId = helper.Id; break;
            case "beneficiary": request.BeneficiaryMemberId = helper.Id; break;
            case "org_admin": helper.User.Role = "org_admin"; break;
        }
        await db.SaveChangesAsync();
        var result = await new SubmitTransferAssistanceOfferHandler(
            db, new AuditService(db), Options.Create(new TransferAssistanceOptions { RequireMfa = false }),
            NullLogger<SubmitTransferAssistanceOfferHandler>.Instance)
            .Handle(new SubmitTransferAssistanceOfferCommand(
                request.TenantId, helper.Id, helper.UserId, request.Id,
                new SubmitTransferAssistanceOfferRequest("Tomorrow", "I can help.")), default);
        Assert.False(result.IsSuccess);
        Assert.Empty(db.TransferAssistanceOffers);
    }

    [Fact]
    public async Task PreviouslyVerifiedOrgAdmins_CannotStartNewAssignments()
    {
        await using var db = TestDbFactory.Create();
        var (request, helper, witness) = await SeedAsync(db, false);
        helper.User.Role = witness.User.Role = "org_admin";
        foreach (var member in new[] { helper, witness })
            db.TransferAssistanceProfiles.Add(new TransferAssistanceProfile
            {
                Id = Guid.NewGuid(), TenantId = request.TenantId, MemberId = member.Id,
                HelperOptedIn = true, WitnessOptedIn = true,
                HelperVerificationStatus = "verified", WitnessVerificationStatus = "verified",
            });
        var offer = new TransferAssistanceOffer
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId, RequestId = request.Id,
            HelperMemberId = helper.Id, Availability = "Tomorrow", Message = "I can help",
        };
        db.TransferAssistanceOffers.Add(offer);
        await db.SaveChangesAsync();
        var options = Options.Create(new TransferAssistanceOptions { RequireMfa = false });
        var audit = new AuditService(db);
        var available = await new ListAvailableTransferAssistanceRequestsHandler(
            db, NullLogger<ListAvailableTransferAssistanceRequestsHandler>.Instance, options)
            .Handle(new(request.TenantId, helper.Id), default);
        Assert.Empty(available);
        var selection = await new SelectTransferAssistanceOfferHandler(
            db, audit, options, NullLogger<SelectTransferAssistanceOfferHandler>.Instance)
            .Handle(new(request.TenantId, request.RequesterMemberId, request.RequesterMember.UserId,
                request.Id, offer.Id), default);
        Assert.Equal("errors.transfer_assistance.helper_not_verified", selection.ErrorCode);
        var candidates = await new SearchTransferAssistanceWitnessesHandler(
            db, options, NullLogger<SearchTransferAssistanceWitnessesHandler>.Instance)
            .Handle(new(request.TenantId, request.RequesterMemberId, "Witness"), default);
        Assert.Empty(candidates);
        request.HelperMemberId = helper.Id;
        request.Status = TransferAssistanceStatuses.WitnessPending;
        await db.SaveChangesAsync();
        var invite = await new InviteTransferAssistanceWitnessHandler(
            db, audit, options, NullLogger<InviteTransferAssistanceWitnessHandler>.Instance)
            .Handle(new(request.TenantId, request.RequesterMemberId, request.RequesterMember.UserId,
                request.Id, new(witness.Id)), default);
        Assert.Equal("errors.transfer_assistance.member_not_eligible", invite.ErrorCode);
        var pending = new TransferAssistanceWitnessInvitation
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId, RequestId = request.Id,
            WitnessMemberId = witness.Id, ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.TransferAssistanceWitnessInvitations.Add(pending);
        await db.SaveChangesAsync();
        var accept = await new RespondTransferAssistanceWitnessInvitationHandler(
            db, audit, options, NullLogger<RespondTransferAssistanceWitnessInvitationHandler>.Instance)
            .Handle(new(request.TenantId, witness.Id, witness.UserId, pending.Id, new(true, false, null)), default);
        Assert.Equal("errors.transfer_assistance.admin_participation_forbidden", accept.ErrorCode);
    }

    private static async Task<(TransferAssistanceRequest Request, Member Helper, Member Witness)> SeedAsync(
        AppDbContext db, bool mfaEnabled)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test community", Slug = "test" };
        db.Tenants.Add(tenant);
        Member AddMember(string name)
        {
            var user = new User
            {
                Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = name, LastName = "Member",
                Email = $"{name.ToLowerInvariant()}@example.test", EmailVerifiedAt = DateTime.UtcNow,
                MfaEnabled = mfaEnabled,
            };
            var member = new Member
            {
                Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id,
                User = user, Status = "active", MembershipNumber = name,
            };
            db.Users.Add(user);
            db.Members.Add(member);
            return member;
        }
        var requester = AddMember("Requester");
        var beneficiary = AddMember("Beneficiary");
        var helper = AddMember("Helper");
        var witness = AddMember("Witness");
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, RequesterMemberId = requester.Id,
            BeneficiaryMemberId = beneficiary.Id, BeneficiaryName = "Beneficiary Member",
            DestinationCountry = "CM", AmountMur = 1000, ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money", Purpose = "Family support", BeneficiaryRelationship = "Parent",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.Matching, ExpiresAt = DateTime.UtcNow.AddDays(2),
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        return (request, helper, witness);
    }
}
