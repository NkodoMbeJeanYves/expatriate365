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

public class TransferAssistanceSimplifiedWorkflowTests
{
    private static readonly IOptions<TransferAssistanceOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new TransferAssistanceOptions
        {
            RequireMfa = false,
            EmailNotificationsEnabled = false,
        });

    [Fact]
    public async Task SelectingOffer_AssignsIntermediaryWithoutWitnessStage()
    {
        await using var db = TestDbFactory.Create();
        var seed = await CreateSeedAsync(db, TransferAssistanceStatuses.Matching);
        db.TransferAssistanceProfiles.Add(new TransferAssistanceProfile
        {
            Id = Guid.NewGuid(),
            TenantId = seed.TenantId,
            MemberId = seed.Intermediary.Id,
            HelperOptedIn = true,
            HelperVerificationStatus = TransferAssistanceVerificationStatuses.Verified,
        });
        var offer = new TransferAssistanceOffer
        {
            Id = Guid.NewGuid(),
            TenantId = seed.TenantId,
            RequestId = seed.Request.Id,
            Request = seed.Request,
            HelperMemberId = seed.Intermediary.Id,
            HelperMember = seed.Intermediary,
            Availability = "Tomorrow",
            Message = "I can coordinate the transfer.",
        };
        db.TransferAssistanceOffers.Add(offer);
        await db.SaveChangesAsync();

        var result = await new SelectTransferAssistanceOfferHandler(
                db, new AuditService(db), Options,
                NullLogger<SelectTransferAssistanceOfferHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Requester.UserId, seed.Request.Id, offer.Id), default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.IntermediaryAssigned, seed.Request.Status);
        Assert.Equal(seed.Intermediary.Id, seed.Request.HelperMemberId);
        Assert.Null(seed.Request.WitnessMemberId);
    }

    [Fact]
    public async Task AssignedIntermediaryCanScheduleMeetingWithoutWitness()
    {
        await using var db = TestDbFactory.Create();
        var seed = await CreateSeedAsync(db, TransferAssistanceStatuses.IntermediaryAssigned);
        seed.Request.HelperMemberId = seed.Intermediary.Id;
        await db.SaveChangesAsync();

        var result = await new ScheduleTransferAssistanceMeetingHandler(
                db, new AuditService(db), Options,
                NullLogger<ScheduleTransferAssistanceMeetingHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Requester.Id, seed.Requester.UserId, seed.Request.Id,
                new(DateTime.UtcNow.AddHours(1))), default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.MeetingScheduled, seed.Request.Status);
        Assert.Null(seed.Request.WitnessMemberId);
    }

    [Fact]
    public async Task RequesterAndIntermediaryConfirmationsAdvanceWithoutWitness()
    {
        await using var db = TestDbFactory.Create();
        var seed = await CreateSeedAsync(db, TransferAssistanceStatuses.MeetingScheduled);
        seed.Request.HelperMemberId = seed.Intermediary.Id;
        seed.Request.MeetingAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var handler = new ConfirmTransferAssistanceHandoverHandler(
            db, new AuditService(db), NullLogger<ConfirmTransferAssistanceHandoverHandler>.Instance);

        var requesterResult = await handler.Handle(new(seed.TenantId, seed.Requester.Id,
            seed.Requester.UserId, seed.Request.Id, new(true)), default);
        Assert.True(requesterResult.IsSuccess, requesterResult.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.MeetingScheduled, seed.Request.Status);

        var intermediaryResult = await handler.Handle(new(seed.TenantId, seed.Intermediary.Id,
            seed.Intermediary.UserId, seed.Request.Id, new(true)), default);

        Assert.True(intermediaryResult.IsSuccess, intermediaryResult.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.TransferInProgress, seed.Request.Status);
        Assert.False(seed.Request.WitnessAttested);
    }

    [Fact]
    public async Task IntermediaryCannotDeclareTransferBeforeBeneficiaryIsDesignated()
    {
        await using var db = TestDbFactory.Create();
        var seed = await CreateSeedAsync(db, TransferAssistanceStatuses.TransferInProgress);
        seed.Request.HelperMemberId = seed.Intermediary.Id;
        await db.SaveChangesAsync();

        var result = await new SubmitTransferAssistanceEvidenceHandler(
                db, new AuditService(db), Options,
                NullLogger<SubmitTransferAssistanceEvidenceHandler>.Instance)
            .Handle(new(seed.TenantId, seed.Intermediary.Id, seed.Intermediary.UserId, seed.Request.Id,
                new("Provider", "reference-123")), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("errors.transfer_assistance.beneficiary_required_before_transfer", result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.TransferInProgress, seed.Request.Status);
    }

    [Fact]
    public async Task AvailableRequests_IndicateWhenCurrentMemberAlreadyOffered()
    {
        await using var db = TestDbFactory.Create();
        var seed = await CreateSeedAsync(db, TransferAssistanceStatuses.Matching);
        db.TransferAssistanceProfiles.Add(new TransferAssistanceProfile
        {
            Id = Guid.NewGuid(),
            TenantId = seed.TenantId,
            MemberId = seed.Intermediary.Id,
            HelperOptedIn = true,
            HelperVerificationStatus = TransferAssistanceVerificationStatuses.Verified,
        });
        await db.SaveChangesAsync();
        var handler = new ListAvailableTransferAssistanceRequestsHandler(
            db, NullLogger<ListAvailableTransferAssistanceRequestsHandler>.Instance, Options);
        var query = new ListAvailableTransferAssistanceRequestsQuery(seed.TenantId, seed.Intermediary.Id);

        var beforeOffer = Assert.Single(await handler.Handle(query, default));
        Assert.False(beforeOffer.MyOfferSubmitted);

        db.TransferAssistanceOffers.Add(new TransferAssistanceOffer
        {
            Id = Guid.NewGuid(),
            TenantId = seed.TenantId,
            RequestId = seed.Request.Id,
            Request = seed.Request,
            HelperMemberId = seed.Intermediary.Id,
            HelperMember = seed.Intermediary,
            Availability = "Tomorrow",
            Message = "I can coordinate the transfer.",
        });
        await db.SaveChangesAsync();

        var afterOffer = Assert.Single(await handler.Handle(query, default));
        Assert.True(afterOffer.MyOfferSubmitted);
    }

    private static async Task<Seed> CreateSeedAsync(AppDbContext db, string status)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test community", Slug = $"workflow-{Guid.NewGuid():N}" };
        db.Tenants.Add(tenant);
        var requester = AddMember(db, tenant.Id, "Requester");
        var intermediary = AddMember(db, tenant.Id, "Intermediary");
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RequesterMemberId = requester.Id,
            RequesterMember = requester,
            DestinationCountry = "CM",
            AmountMur = 1000,
            ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money",
            Purpose = "Family support",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            MeetingArea = "Ebene",
            Status = status,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        return new(tenant.Id, request, requester, intermediary);
    }

    private static Member AddMember(AppDbContext db, Guid tenantId, string name)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = name,
            LastName = "Member",
            Email = $"{name.ToLowerInvariant()}@example.test",
            EmailVerifiedAt = DateTime.UtcNow,
        };
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            User = user,
            MembershipNumber = $"M-{Guid.NewGuid():N}",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Users.Add(user);
        db.Members.Add(member);
        return member;
    }

    private sealed record Seed(Guid TenantId, TransferAssistanceRequest Request, Member Requester, Member Intermediary);
}
