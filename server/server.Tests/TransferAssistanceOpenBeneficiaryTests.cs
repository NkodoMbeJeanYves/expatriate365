using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Application.TransferAssistance.Queries;
using server.Domain.Entities;
using server.Infrastructure.BackgroundServices;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Tests;

public class TransferAssistanceOpenBeneficiaryTests
{
    private static readonly IOptions<TransferAssistanceOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new TransferAssistanceOptions
        {
            RequireMfa = false, EmailNotificationsEnabled = true, EmailThrottleMinutes = 10,
        });

    [Fact]
    public async Task CreateRequest_WithoutBeneficiary_IsAccepted()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var options = Microsoft.Extensions.Options.Options.Create(new TransferAssistanceOptions
        {
            RequireMfa = false, MinimumRequestAmountMur = 1, MaximumRequestAmountMur = 100000,
        });
        var result = await new CreateTransferAssistanceRequestHandler(
            db, new AuditService(db), new CreateTransferAssistanceRequestValidator(), options,
            NullLogger<CreateTransferAssistanceRequestHandler>.Instance)
            .Handle(new CreateTransferAssistanceRequestCommand(seed.TenantId, seed.CandidateB.Id,
                new CreateTransferAssistanceRequest(
                    null, "CM", 15000, "XAF", "mobile_money", "Hospital fees", "",
                    DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    "Ebene")), default);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Null(result.Data!.BeneficiaryMemberId);
        Assert.True(result.Data.BeneficiaryPending);
    }

    [Fact]
    public async Task ApplyThenDesignate_SelectsCandidate_DeclinesOthers_AndNotifies()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var first = await ApplyAsync(db, seed, seed.CandidateA);
        var second = await ApplyAsync(db, seed, seed.CandidateB);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, db.Notifications.Count(n => n.UserId == seed.Requester.UserId &&
            n.Type == TransferAssistanceNotificationTypes.BeneficiaryCandidate));

        var list = await new ListTransferAssistanceBeneficiaryCandidatesHandler(
            db, NullLogger<ListTransferAssistanceBeneficiaryCandidatesHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Request.Id), default);
        Assert.Equal([seed.CandidateA.Id, seed.CandidateB.Id], list.Data!.Select(c => c.MemberId));

        var designated = await DesignateAsync(db, seed, new(first.Data!.Id, null, "Friend"));
        Assert.True(designated.IsSuccess, designated.ErrorCode);
        Assert.Equal(seed.CandidateA.Id, seed.Request.BeneficiaryMemberId);
        Assert.Equal("Friend", seed.Request.BeneficiaryRelationship);
        Assert.False(designated.Data!.BeneficiaryPending);
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Selected,
            db.TransferAssistanceBeneficiaryCandidates.Single(c => c.MemberId == seed.CandidateA.Id).Status);
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Declined,
            db.TransferAssistanceBeneficiaryCandidates.Single(c => c.MemberId == seed.CandidateB.Id).Status);
        Assert.Contains(db.Notifications, n => n.UserId == seed.CandidateA.UserId &&
            n.Type == TransferAssistanceNotificationTypes.BeneficiaryDesignated);
        Assert.Contains(db.Notifications, n => n.UserId == seed.CandidateB.UserId &&
            n.Type == TransferAssistanceNotificationTypes.BeneficiaryCandidateDeclined);
        Assert.Contains(db.TransferAssistanceEvents, e => e.EventType == "beneficiary_designated");

        // Idempotent for the same member, conflict for another one.
        var again = await DesignateAsync(db, seed, new(null, seed.CandidateA.Id, "Friend"));
        Assert.True(again.IsSuccess);
        var other = await DesignateAsync(db, seed, new(null, seed.CandidateB.Id, null));
        Assert.Equal("errors.transfer_assistance.beneficiary_already_designated", other.ErrorCode);
    }

    [Fact]
    public async Task Designate_RejectsHelper_AndAmbiguousPayload()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Request.HelperMemberId = seed.Helper.Id;
        seed.Request.Status = TransferAssistanceStatuses.WitnessPending;
        await db.SaveChangesAsync();

        var helper = await DesignateAsync(db, seed, new(null, seed.Helper.Id, null));
        Assert.Equal("errors.transfer_assistance.conflict_of_interest", helper.ErrorCode);
        var requester = await DesignateAsync(db, seed, new(null, seed.Requester.Id, null));
        Assert.Equal("errors.transfer_assistance.conflict_of_interest", requester.ErrorCode);
        var ambiguous = await DesignateAsync(db, seed, new(Guid.NewGuid(), seed.CandidateA.Id, null));
        Assert.Equal("errors.validation.invalid", ambiguous.ErrorCode);

        var designated = await DesignateAsync(db, seed, new(null, seed.CandidateA.Id, null));
        Assert.True(designated.IsSuccess, designated.ErrorCode);
    }

    [Fact]
    public async Task Designate_IsForbiddenToNonOwner()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var result = await new DesignateTransferAssistanceBeneficiaryHandler(
            db, new AuditService(db), Options, NullLogger<DesignateTransferAssistanceBeneficiaryHandler>.Instance)
            .Handle(new(seed.TenantId, seed.CandidateA.Id, seed.CandidateA.UserId, seed.Request.Id,
                new(null, seed.CandidateB.Id, null)), default);
        Assert.Equal("errors.transfer_assistance.not_found", result.ErrorCode);
    }

    [Fact]
    public async Task InviteWitness_RequiresDesignatedBeneficiary()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Request.HelperMemberId = seed.Helper.Id;
        seed.Request.Status = TransferAssistanceStatuses.WitnessPending;
        await db.SaveChangesAsync();
        var result = await new InviteTransferAssistanceWitnessHandler(
            db, new AuditService(db), Options, NullLogger<InviteTransferAssistanceWitnessHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Requester.UserId, seed.Request.Id,
                new(seed.CandidateB.Id)), default);
        Assert.Equal("errors.transfer_assistance.beneficiary_required_before_witness", result.ErrorCode);
    }

    [Fact]
    public async Task HelperAndCandidateRoles_AreMutuallyExclusive()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        db.TransferAssistanceProfiles.Add(new TransferAssistanceProfile
        {
            Id = Guid.NewGuid(), TenantId = seed.TenantId, MemberId = seed.Helper.Id,
            HelperOptedIn = true, HelperVerificationStatus = TransferAssistanceVerificationStatuses.Verified,
        });
        db.TransferAssistanceProfiles.Add(new TransferAssistanceProfile
        {
            Id = Guid.NewGuid(), TenantId = seed.TenantId, MemberId = seed.CandidateA.Id,
            HelperOptedIn = true, HelperVerificationStatus = TransferAssistanceVerificationStatuses.Verified,
        });
        await db.SaveChangesAsync();
        var offerHandler = new SubmitTransferAssistanceOfferHandler(
            db, new AuditService(db), Options, NullLogger<SubmitTransferAssistanceOfferHandler>.Instance);

        var offer = await offerHandler.Handle(new(seed.TenantId, seed.Helper.Id, seed.Helper.UserId,
            seed.Request.Id, new("Tomorrow", "I can help.")), default);
        Assert.True(offer.IsSuccess, offer.ErrorCode);
        var helperApplies = await ApplyAsync(db, seed, seed.Helper);
        Assert.Equal("errors.transfer_assistance.beneficiary_candidate_conflict", helperApplies.ErrorCode);

        Assert.True((await ApplyAsync(db, seed, seed.CandidateA)).IsSuccess);
        var candidateOffers = await offerHandler.Handle(new(seed.TenantId, seed.CandidateA.Id,
            seed.CandidateA.UserId, seed.Request.Id, new("Tomorrow", "I can help.")), default);
        Assert.Equal("errors.transfer_assistance.beneficiary_candidate_conflict", candidateOffers.ErrorCode);
    }

    [Fact]
    public async Task OpenList_IsAnonymized_ExcludesOwnRequest_AndShowsMyCandidacy()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var handler = new ListBeneficiaryOpenTransferAssistanceRequestsHandler(
            db, NullLogger<ListBeneficiaryOpenTransferAssistanceRequestsHandler>.Instance);

        Assert.Empty(await handler.Handle(new(seed.TenantId, seed.Requester.Id), default));
        var before = Assert.Single(await handler.Handle(new(seed.TenantId, seed.CandidateA.Id), default));
        Assert.Null(before.MyCandidateStatus);
        Assert.DoesNotContain(typeof(TransferAssistanceBeneficiaryOpenRequestResponse).GetProperties(),
            p => p.Name.Contains("Requester") || p.Name.Contains("MeetingArea"));

        await ApplyAsync(db, seed, seed.CandidateA);
        var after = Assert.Single(await handler.Handle(new(seed.TenantId, seed.CandidateA.Id), default));
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Pending, after.MyCandidateStatus);

        await DesignateAsync(db, seed, new(null, seed.CandidateA.Id, null));
        Assert.Empty(await handler.Handle(new(seed.TenantId, seed.CandidateB.Id), default));
    }

    [Fact]
    public async Task BeneficiaryCanBeDesignatedAfterIntermediaryConfirmsHandover()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Request.HelperMemberId = seed.Helper.Id;
        seed.Request.Status = TransferAssistanceStatuses.TransferInProgress;
        await db.SaveChangesAsync();

        var openRequest = Assert.Single(await new ListBeneficiaryOpenTransferAssistanceRequestsHandler(
                db, NullLogger<ListBeneficiaryOpenTransferAssistanceRequestsHandler>.Instance)
            .Handle(new(seed.TenantId, seed.CandidateA.Id), default));
        Assert.Equal(seed.Request.Id, openRequest.Id);
        var candidacy = await ApplyAsync(db, seed, seed.CandidateA);
        Assert.True(candidacy.IsSuccess, candidacy.ErrorCode);

        var designated = await DesignateAsync(db, seed, new(candidacy.Data!.Id, null, "Friend"));

        Assert.True(designated.IsSuccess, designated.ErrorCode);
        Assert.Equal(seed.CandidateA.Id, seed.Request.BeneficiaryMemberId);
    }

    [Fact]
    public async Task Withdraw_ThenReapply_ReusesCandidacy()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var applied = await ApplyAsync(db, seed, seed.CandidateA);
        var withdraw = await new WithdrawTransferAssistanceBeneficiaryHandler(
            db, new AuditService(db), NullLogger<WithdrawTransferAssistanceBeneficiaryHandler>.Instance)
            .Handle(new(seed.TenantId, seed.CandidateA.Id, seed.CandidateA.UserId, seed.Request.Id), default);
        Assert.True(withdraw.IsSuccess);
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Withdrawn,
            db.TransferAssistanceBeneficiaryCandidates.Single().Status);

        var reapplied = await ApplyAsync(db, seed, seed.CandidateA);
        Assert.True(reapplied.IsSuccess);
        Assert.Equal(applied.Data!.Id, reapplied.Data!.Id);
        Assert.Single(db.TransferAssistanceBeneficiaryCandidates);
    }

    [Fact]
    public async Task Cancel_ClosesPendingCandidacies()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        await ApplyAsync(db, seed, seed.CandidateA);
        var cancelled = await new CancelTransferAssistanceRequestHandler(
            db, new AuditService(db), NullLogger<CancelTransferAssistanceRequestHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Requester.UserId, seed.Request.Id), default);
        Assert.True(cancelled.IsSuccess, cancelled.ErrorCode);
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Declined,
            db.TransferAssistanceBeneficiaryCandidates.Single().Status);
    }

    [Fact]
    public async Task Notification_EnqueuesEmailOnlyToContactEmail_AndRespectsPreferences()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Requester.User.ContactEmail = "requester.personal@example.test";
        await db.SaveChangesAsync();

        await ApplyAsync(db, seed, seed.CandidateA);
        var message = Assert.Single(db.EmailOutbox);
        Assert.Equal("requester.personal@example.test", message.ToEmail);
        Assert.Equal(EmailOutboxTemplates.TransferAssistanceEvent, message.Template);
        Assert.Contains("beneficiary_candidate", message.PayloadJson);
        Assert.DoesNotContain("CandidateA", message.PayloadJson);

        // Same event within the throttle window is deduplicated.
        await ApplyAsync(db, seed, seed.CandidateB);
        Assert.Single(db.EmailOutbox);

        // Disabling the email channel keeps the in-app notification only.
        db.EmailOutbox.RemoveRange(db.EmailOutbox);
        db.NotificationPreferences.Add(new NotificationPreference
        {
            TenantId = seed.TenantId, UserId = seed.CandidateA.UserId,
            NotificationType = TransferAssistanceNotificationTypes.EmailChannel, Enabled = false,
        });
        seed.CandidateA.User.ContactEmail = "a.personal@example.test";
        await db.SaveChangesAsync();
        await DesignateAsync(db, seed, new(null, seed.CandidateA.Id, null));
        Assert.Contains(db.Notifications, n => n.UserId == seed.CandidateA.UserId &&
            n.Type == TransferAssistanceNotificationTypes.BeneficiaryDesignated);
        Assert.DoesNotContain(db.EmailOutbox, m => m.UserId == seed.CandidateA.UserId);
        // Candidate B has no contact email: no email even though the event applies.
        Assert.DoesNotContain(db.EmailOutbox, m => m.UserId == seed.CandidateB.UserId);
    }

    [Fact]
    public async Task NotifyWithEmail_UsesContactEmail_NotLoginEmail()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        await service.NotifyWithEmailAsync(seed.TenantId, seed.Requester.UserId, "contribution_reminder",
            "Title", "Body", "Subject", "<p>Hi</p>");
        Assert.Single(db.Notifications);
        Assert.Empty(db.EmailOutbox);

        seed.Requester.User.ContactEmail = "personal@example.test";
        await db.SaveChangesAsync();
        await service.NotifyWithEmailAsync(seed.TenantId, seed.Requester.UserId, "contribution_reminder",
            "Title", "Body", "Subject", "<p>Hi</p>");
        var message = Assert.Single(db.EmailOutbox);
        Assert.Equal("personal@example.test", message.ToEmail);
        Assert.NotEqual(seed.Requester.User.Email, message.ToEmail);
        Assert.Equal(EmailOutboxTemplates.PreRenderedHtml, message.Template);
    }

    [Theory]
    [InlineData(EmailSendResult.Sent, EmailOutboxStatuses.Sent)]
    [InlineData(EmailSendResult.NotConfigured, EmailOutboxStatuses.Skipped)]
    public async Task Processor_HandlesSendResults(EmailSendResult sendResult, string expectedStatus)
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Requester.User.ContactEmail = "personal@example.test";
        seed.Requester.User.PreferredLanguage = "en";
        var message = await EnqueueAsync(db, seed);
        var sender = new RecordingEmailService(sendResult);

        await CreateProcessor(db, sender).ProcessBatchAsync(default);

        Assert.Equal(expectedStatus, message.Status);
        var sent = Assert.Single(sender.Sent);
        Assert.Equal("personal@example.test", sent.To);
        Assert.Contains("Test community", sent.Subject + sent.Html);
        Assert.DoesNotContain("15000", sent.Html);
    }

    [Fact]
    public async Task Processor_RetriesTransientFailures_ThenFails()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Requester.User.ContactEmail = "personal@example.test";
        var message = await EnqueueAsync(db, seed);
        var processor = CreateProcessor(db, new RecordingEmailService(EmailSendResult.TransientFailure), maxAttempts: 2);

        await processor.ProcessBatchAsync(default);
        Assert.Equal(EmailOutboxStatuses.Pending, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.True(message.NextAttemptAt > DateTime.UtcNow);

        message.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await processor.ProcessBatchAsync(default);
        Assert.Equal(EmailOutboxStatuses.Failed, message.Status);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public async Task Processor_SkipsWhenContactEmailWasRemoved()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        seed.Requester.User.ContactEmail = "personal@example.test";
        var message = await EnqueueAsync(db, seed);
        seed.Requester.User.ContactEmail = null;
        await db.SaveChangesAsync();
        var sender = new RecordingEmailService(EmailSendResult.Sent);

        await CreateProcessor(db, sender).ProcessBatchAsync(default);

        Assert.Equal(EmailOutboxStatuses.Skipped, message.Status);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task SyncState_ChangesWhenVisibleDataChanges_AndOmitsForbiddenSections()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        var handler = new GetTransferAssistanceSyncStateHandler(
            db, NullLogger<GetTransferAssistanceSyncStateHandler>.Instance);
        string[] memberPermissions =
        [
            Permissions.TransferAssistanceReadOwn, Permissions.TransferAssistanceViewAvailable,
            Permissions.TransferAssistanceParticipate,
        ];

        var before = await handler.Handle(new(seed.TenantId, seed.Requester.Id, memberPermissions), default);
        Assert.DoesNotContain(TransferAssistanceSyncSections.ReviewQueue, before.Sections.Keys);
        Assert.Contains(TransferAssistanceSyncSections.Mine, before.Sections.Keys);
        var same = await handler.Handle(new(seed.TenantId, seed.Requester.Id, memberPermissions), default);
        Assert.Equal(before.Version, same.Version);

        await ApplyAsync(db, seed, seed.CandidateA);
        var after = await handler.Handle(new(seed.TenantId, seed.Requester.Id, memberPermissions), default);
        Assert.NotEqual(before.Sections[TransferAssistanceSyncSections.Mine],
            after.Sections[TransferAssistanceSyncSections.Mine]);
        Assert.NotEqual(before.Version, after.Version);

        var none = await handler.Handle(new(seed.TenantId, seed.Requester.Id, []), default);
        Assert.Empty(none.Sections);
        var reviewer = await handler.Handle(new(seed.TenantId, null, [Permissions.TransferAssistanceReview]), default);
        Assert.Equal([TransferAssistanceSyncSections.ReviewQueue], reviewer.Sections.Keys);
    }

    [Fact]
    public async Task Expiration_WarnsOnce_ThenExpiresAndClosesCandidacies()
    {
        await using var db = TestDbFactory.Create();
        var seed = await SeedAsync(db);
        await ApplyAsync(db, seed, seed.CandidateA);
        seed.Request.ExpiresAt = DateTime.UtcNow.AddHours(12);
        await db.SaveChangesAsync();

        await TransferAssistanceExpirationService.ExpireWorkflowItemsAsync(
            db, Options.Value, NullLogger.Instance, default);
        await TransferAssistanceExpirationService.ExpireWorkflowItemsAsync(
            db, Options.Value, NullLogger.Instance, default);
        Assert.Single(db.TransferAssistanceEvents,
            e => e.EventType == TransferAssistanceExpirationService.ExpiryWarningEvent);

        seed.Request.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await TransferAssistanceExpirationService.ExpireWorkflowItemsAsync(
            db, Options.Value, NullLogger.Instance, default);
        Assert.Equal(TransferAssistanceStatuses.Expired, seed.Request.Status);
        Assert.Equal(TransferAssistanceBeneficiaryCandidateStatuses.Declined,
            db.TransferAssistanceBeneficiaryCandidates.Single().Status);
        Assert.Contains(db.Notifications, n => n.UserId == seed.Requester.UserId &&
            n.Type == TransferAssistanceNotificationTypes.Expiration);
    }

    private static Task<ServiceResult<TransferAssistanceBeneficiaryCandidateResponse>> ApplyAsync(
        AppDbContext db, Seed seed, Member member) =>
        new ApplyTransferAssistanceBeneficiaryHandler(
            db, new AuditService(db), Options, NullLogger<ApplyTransferAssistanceBeneficiaryHandler>.Instance)
            .Handle(new(seed.TenantId, member.Id, member.UserId, seed.Request.Id,
                new ApplyTransferAssistanceBeneficiaryRequest("I can receive it.")), default);

    private static Task<ServiceResult<TransferAssistanceRequestResponse>> DesignateAsync(
        AppDbContext db, Seed seed, DesignateTransferAssistanceBeneficiaryRequest body) =>
        new DesignateTransferAssistanceBeneficiaryHandler(
            db, new AuditService(db), Options, NullLogger<DesignateTransferAssistanceBeneficiaryHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Requester.UserId, seed.Request.Id, body), default);

    private static async Task<EmailOutboxMessage> EnqueueAsync(AppDbContext db, Seed seed)
    {
        await EmailOutbox.EnqueueAsync(db, seed.TenantId, seed.Requester.UserId, "personal@example.test",
            "Requester Member", "fr", EmailOutboxTemplates.TransferAssistanceEvent,
            new Dictionary<string, string> { ["event"] = "beneficiary_candidate", ["request_id"] = seed.Request.Id.ToString() },
            $"test:{Guid.NewGuid():N}", default);
        await db.SaveChangesAsync();
        return db.EmailOutbox.Single();
    }

    private static EmailOutboxProcessor CreateProcessor(AppDbContext db, IEmailService sender, int maxAttempts = 5) =>
        new(db, sender,
            Microsoft.Extensions.Options.Options.Create(new EmailOutboxOptions { MaxAttempts = maxAttempts }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendBaseUrl"] = "https://app.example.test",
            }).Build(),
            NullLogger<EmailOutboxProcessor>.Instance);

    private sealed class RecordingEmailService(EmailSendResult result) : IEmailService
    {
        public List<(string To, string Subject, string Html)> Sent { get; } = [];

        public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<EmailSendResult> TrySendAsync(
            string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
        {
            Sent.Add((toEmail, subject, htmlBody));
            return Task.FromResult(result);
        }
    }

    private sealed record Seed(
        Guid TenantId,
        TransferAssistanceRequest Request,
        Member Requester,
        Member Helper,
        Member CandidateA,
        Member CandidateB);

    private static async Task<Seed> SeedAsync(AppDbContext db)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test community", Slug = "test" };
        db.Tenants.Add(tenant);
        Member AddMember(string name)
        {
            var user = new User
            {
                Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = name, LastName = "Member",
                Email = $"{name.ToLowerInvariant()}@association.test", EmailVerifiedAt = DateTime.UtcNow,
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
        var helper = AddMember("Helper");
        var candidateA = AddMember("CandidateA");
        var candidateB = AddMember("CandidateB");
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, RequesterMemberId = requester.Id,
            RequesterMember = requester, DestinationCountry = "CM", AmountMur = 15000, ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money", Purpose = "Hospital fees",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.Matching, ExpiresAt = DateTime.UtcNow.AddDays(2),
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        return new Seed(tenant.Id, request, requester, helper, candidateA, candidateB);
    }
}
