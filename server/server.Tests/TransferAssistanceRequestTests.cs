using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Tests;

public class TransferAssistanceRequestTests
{
    [Theory]
    [InlineData("bank_transfer")]
    [InlineData("mobile_money")]
    [InlineData("orange_money")]
    public void CreateRequestValidator_AcceptsSupportedReceiveMethods(string receiveMethod)
    {
        var request = ValidRequest() with { ReceiveMethod = receiveMethod };
        var command = new CreateTransferAssistanceRequestCommand(Guid.NewGuid(), Guid.NewGuid(), request);

        var result = new CreateTransferAssistanceRequestValidator().Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateRequestValidator_RejectsUnsupportedReceiveMethods()
    {
        var request = ValidRequest() with { ReceiveMethod = "unsupported" };
        var command = new CreateTransferAssistanceRequestCommand(Guid.NewGuid(), Guid.NewGuid(), request);

        var result = new CreateTransferAssistanceRequestValidator().Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == "Request.ReceiveMethod");
    }

    [Fact]
    public async Task CreateRequest_RequiresAnActiveMemberInTheSameTenant()
    {
        await using var db = TestDbFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "member@example.com",
            FirstName = "Test",
            LastName = "Member",
            IsActive = true
        };
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" });
        db.Users.Add(user);
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            MembershipNumber = "M-001",
            Status = "suspended",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await db.SaveChangesAsync();

        var handler = new CreateTransferAssistanceRequestHandler(
            db,
            new AuditService(db),
            new CreateTransferAssistanceRequestValidator(),
            Options.Create(new TransferAssistanceOptions
            {
                MinimumRequestAmountMur = 1,
                MaximumRequestAmountMur = 10000
            }),
            NullLogger<CreateTransferAssistanceRequestHandler>.Instance);
        var result = await handler.Handle(
            new CreateTransferAssistanceRequestCommand(tenantId, db.Members.Single().Id, ValidRequest()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("errors.transfer_assistance.member_not_eligible", result.ErrorCode);
    }

    [Theory]
    [InlineData(true, "errors.transfer_assistance.not_found")]
    [InlineData(false, "errors.transfer_assistance.member_not_eligible")]
    public async Task SubmitRequest_RejectsOtherOrMissingMembers(bool useEligibleMember, string expectedError)
    {
        await using var db = TestDbFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "member@example.com",
            FirstName = "Test",
            LastName = "Member"
        };
        var beneficiaryUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "beneficiary@example.com",
            FirstName = "Test",
            LastName = "Beneficiary",
            EmailVerifiedAt = DateTime.UtcNow
        };
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" });
        db.Users.AddRange(user, beneficiaryUser);
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            MembershipNumber = "M-001",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        var beneficiary = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = beneficiaryUser.Id,
            MembershipNumber = "M-002",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "other-member@example.com",
            FirstName = "Other",
            LastName = "Member"
        };
        var otherMember = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = otherUser.Id,
            MembershipNumber = "M-003",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        db.Users.Add(otherUser);
        db.Members.Add(otherMember);
        db.Members.Add(member);
        db.Members.Add(beneficiary);
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequesterMemberId = member.Id,
            BeneficiaryMemberId = beneficiary.Id,
            BeneficiaryName = "Beneficiary",
            DestinationCountry = "CM",
            AmountMur = 1000,
            ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money",
            Purpose = "Family support",
            BeneficiaryRelationship = "Parent",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.Draft
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new SubmitTransferAssistanceRequestHandler(
            db,
            new AuditService(db),
            Options.Create(new TransferAssistanceOptions
            {
                MinimumRequestAmountMur = 1,
                MaximumRequestAmountMur = 10000
            }),
            NullLogger<SubmitTransferAssistanceRequestHandler>.Instance);
        var result = await handler.Handle(
            new SubmitTransferAssistanceRequestCommand(
                tenantId, useEligibleMember ? otherMember.Id : Guid.NewGuid(), otherUser.Id, request.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedError, result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.Draft, request.Status);
    }

    [Fact]
    public async Task ReviewRequest_RejectsCrossTenantAccess()
    {
        await using var db = TestDbFactory.Create();
        var requestTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = requestTenantId,
            Email = "member@example.com",
            FirstName = "Test",
            LastName = "Member"
        };
        db.Tenants.AddRange(
            new Tenant { Id = requestTenantId, Name = "Request Tenant", Slug = "request-tenant" },
            new Tenant { Id = otherTenantId, Name = "Other Tenant", Slug = "other-tenant" });
        db.Users.Add(user);
        var beneficiaryUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = requestTenantId,
            Email = "beneficiary@example.com",
            FirstName = "Test",
            LastName = "Beneficiary",
            EmailVerifiedAt = DateTime.UtcNow
        };
        db.Users.Add(beneficiaryUser);
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = requestTenantId,
            UserId = user.Id,
            MembershipNumber = "M-001",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        var beneficiary = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = requestTenantId,
            UserId = beneficiaryUser.Id,
            MembershipNumber = "M-002",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        db.Members.Add(member);
        db.Members.Add(beneficiary);
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = requestTenantId,
            RequesterMemberId = member.Id,
            BeneficiaryMemberId = beneficiary.Id,
            BeneficiaryName = "Beneficiary",
            DestinationCountry = "CM",
            AmountMur = 1000,
            ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money",
            Purpose = "Family support",
            BeneficiaryRelationship = "Parent",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.Submitted
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new ReviewTransferAssistanceRequestHandler(
            db,
            new AuditService(db),
            new ReviewTransferAssistanceRequestValidator(),
            Options.Create(new TransferAssistanceOptions()),
            NullLogger<ReviewTransferAssistanceRequestHandler>.Instance);
        var result = await handler.Handle(
            new ReviewTransferAssistanceRequestCommand(otherTenantId, Guid.NewGuid(), request.Id,
                new ReviewTransferAssistanceRequest("approve", null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("errors.transfer_assistance.not_found", result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.Submitted, request.Status);
    }

    [Fact]
    public async Task BeneficiaryPartialReceiptOpensADisputeInsteadOfCompleting()
    {
        await using var db = TestDbFactory.Create();
        var (tenantId, _, beneficiary, request) = await SeedTransferSentRequestAsync(db);
        var handler = new ConfirmTransferAssistanceReceiptHandler(
            db,
            new AuditService(db),
            NullLogger<ConfirmTransferAssistanceReceiptHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmTransferAssistanceReceiptCommand(
                tenantId, beneficiary.Id, beneficiary.UserId, request.Id,
                new ConfirmTransferAssistanceReceiptRequest("partial", "Only part arrived.")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransferAssistanceStatuses.Disputed, request.Status);
        Assert.Equal("partial", request.BeneficiaryConfirmation);
        Assert.Equal("Only part arrived.", request.DisputeReason);
        Assert.NotNull(request.DisputeOpenedAt);
        Assert.NotEqual(TransferAssistanceStatuses.Completed, request.Status);
        Assert.Contains(db.TransferAssistanceEvents, e => e.EventType == "beneficiary_confirmed_partial");
    }

    [Fact]
    public async Task BeneficiaryCannotConfirmReceiptBeforeTransferEvidence()
    {
        await using var db = TestDbFactory.Create();
        var (tenantId, _, beneficiary, request) = await SeedTransferSentRequestAsync(db);
        request.Status = TransferAssistanceStatuses.Matching;
        var handler = new ConfirmTransferAssistanceReceiptHandler(
            db,
            new AuditService(db),
            NullLogger<ConfirmTransferAssistanceReceiptHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmTransferAssistanceReceiptCommand(
                tenantId, beneficiary.Id, beneficiary.UserId, request.Id,
                new ConfirmTransferAssistanceReceiptRequest("full", null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("errors.transfer_assistance.invalid_transition", result.ErrorCode);
        Assert.Equal(TransferAssistanceStatuses.Matching, request.Status);
        Assert.Null(request.BeneficiaryConfirmation);
    }

    private static async Task<(Guid TenantId, Member Requester, Member Beneficiary, TransferAssistanceRequest Request)>
        SeedTransferSentRequestAsync(AppDbContext db)
    {
        var tenantId = Guid.NewGuid();
        var requesterUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "requester@example.com",
            FirstName = "Request",
            LastName = "Owner",
            IsActive = true
        };
        var beneficiaryUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "beneficiary@example.com",
            FirstName = "Beneficiary",
            LastName = "Member",
            EmailVerifiedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Slug = $"tenant-{tenantId:N}" });
        db.Users.AddRange(requesterUser, beneficiaryUser);
        var requester = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = requesterUser.Id,
            MembershipNumber = "M-101",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };
        var beneficiary = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = beneficiaryUser.Id,
            MembershipNumber = "M-102",
            Status = "active",
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };
        db.Members.AddRange(requester, beneficiary);
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequesterMemberId = requester.Id,
            BeneficiaryMemberId = beneficiary.Id,
            BeneficiaryName = beneficiaryUser.FullName,
            DestinationCountry = "CM",
            AmountMur = 1000,
            ReceiveCurrency = "XAF",
            ReceiveMethod = "mobile_money",
            Purpose = "Family support",
            BeneficiaryRelationship = "Parent",
            AvailableFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            AvailableTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            MeetingArea = "Ebene",
            Status = TransferAssistanceStatuses.TransferSent,
            TransferProvider = "External provider",
            TransferEvidenceReference = "REF-123",
            TransferSentAt = DateTime.UtcNow,
            OpenRequestKey = $"{tenantId:N}:{requester.Id:N}"
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        return (tenantId, requester, beneficiary, request);
    }

    private static CreateTransferAssistanceRequest ValidRequest() => new(
        Guid.NewGuid(),
        "CM",
        1000,
        "XAF",
        "mobile_money",
        "Family support",
        "Parent",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
        "Ebene");
}
