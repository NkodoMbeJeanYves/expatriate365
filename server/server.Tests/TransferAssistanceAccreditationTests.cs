using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Commands;
using server.Application.TransferAssistance.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Tests;

public class TransferAssistanceAccreditationTests
{
    private static UpdateTransferAssistanceProfileHandler Handler(AppDbContext db) => new(
        db, new AuditService(db), new UpdateTransferAssistanceProfileValidator(),
        Options.Create(new TransferAssistanceOptions { RequireMfa = false }),
        NullLogger<UpdateTransferAssistanceProfileHandler>.Instance);

    private static UpdateTransferAssistanceProfileCommand Command(
        Member member, string role, string action) => new(
        member.TenantId, member.Id, member.UserId,
        new UpdateTransferAssistanceProfileRequest(ParticipantRole: role, Action: action));

    [Theory]
    [InlineData("helper", "pending")]
    [InlineData("helper", "verified")]
    [InlineData("helper", "rejected")]
    [InlineData("witness", "pending")]
    [InlineData("witness", "verified")]
    [InlineData("witness", "rejected")]
    public async Task ExistingAccreditation_MustBeCancelledBeforeRequestingAgain(string role, string status)
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedAsync(db);
        var handler = Handler(db);
        Assert.True((await handler.Handle(Command(member, role, "request"), default)).IsSuccess);
        var profile = await db.TransferAssistanceProfiles.SingleAsync();
        if (role == "helper") profile.HelperVerificationStatus = status;
        else profile.WitnessVerificationStatus = status;
        await db.SaveChangesAsync();
        var auditCount = await db.AuditLogs.CountAsync();

        var duplicate = await handler.Handle(Command(member, role, "request"), default);
        Assert.Equal("errors.transfer_assistance.accreditation_exists", duplicate.ErrorCode);
        Assert.Equal(auditCount, await db.AuditLogs.CountAsync());
        Assert.True((await handler.Handle(Command(member, role, "cancel"), default)).IsSuccess);
        Assert.True((await handler.Handle(Command(member, role, "request"), default)).IsSuccess);
        Assert.Equal("pending", role == "helper" ? profile.HelperVerificationStatus : profile.WitnessVerificationStatus);
        Assert.Single(db.TransferAssistanceProfiles);
    }

    [Theory]
    [InlineData("helper")]
    [InlineData("witness")]
    public async Task OrgAdmin_CannotRequestOrBeAccredited(string role)
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedAsync(db);
        var handler = Handler(db);
        Assert.True((await handler.Handle(Command(member, role, "request"), default)).IsSuccess);
        member.User.Role = "org_admin";
        await db.SaveChangesAsync();
        Assert.Equal("errors.transfer_assistance.admin_participation_forbidden",
            (await handler.Handle(Command(member, role, "request"), default)).ErrorCode);
        var reviewer = new ReviewTransferAssistanceProfileHandler(db, new AuditService(db),
            new ReviewTransferAssistanceProfileValidator(), NullLogger<ReviewTransferAssistanceProfileHandler>.Instance);
        var result = await reviewer.Handle(new ReviewTransferAssistanceProfileCommand(
            member.TenantId, Guid.NewGuid(), member.Id, new(role, "verify", "Reviewed")), default);
        Assert.Equal("errors.transfer_assistance.admin_participation_forbidden", result.ErrorCode);
        Assert.True((await handler.Handle(Command(member, role, "cancel"), default)).IsSuccess);
    }

    [Fact]
    public async Task RolesAreIndependent_AndLegacyRepeatedSaveDoesNotCreateAnotherAudit()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedAsync(db);
        var handler = Handler(db);
        await handler.Handle(Command(member, "helper", "request"), default);
        await handler.Handle(Command(member, "witness", "request"), default);
        var count = await db.AuditLogs.CountAsync();
        var repeated = await handler.Handle(new(member.TenantId, member.Id, member.UserId, new(true, true)), default);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(count, await db.AuditLogs.CountAsync());
        await handler.Handle(Command(member, "helper", "cancel"), default);
        var profile = await db.TransferAssistanceProfiles.SingleAsync();
        Assert.False(profile.HelperOptedIn);
        Assert.True(profile.WitnessOptedIn);
        Assert.Equal("pending", profile.WitnessVerificationStatus);
    }

    [Fact]
    public async Task Cancellation_DoesNotChangeExistingAssignments()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedAsync(db);
        var handler = Handler(db);
        await handler.Handle(Command(member, "helper", "request"), default);
        var request = new TransferAssistanceRequest
        {
            Id = Guid.NewGuid(), TenantId = member.TenantId, RequesterMemberId = Guid.NewGuid(),
            HelperMemberId = member.Id, WitnessMemberId = Guid.NewGuid(),
            Status = TransferAssistanceStatuses.MeetingScheduled,
        };
        db.TransferAssistanceRequests.Add(request);
        await db.SaveChangesAsync();
        Assert.True((await handler.Handle(Command(member, "helper", "cancel"), default)).IsSuccess);
        Assert.Equal(member.Id, request.HelperMemberId);
        Assert.Equal(TransferAssistanceStatuses.MeetingScheduled, request.Status);
    }

    [Fact]
    public async Task ConcurrentReviewAndCancellation_CannotOverwriteEachOther()
    {
        var database = Guid.NewGuid().ToString();
        await using var first = TestDbFactory.Create(database);
        var member = await SeedAsync(first);
        await Handler(first).Handle(Command(member, "helper", "request"), default);
        await using var second = TestDbFactory.Create(database);
        await second.TransferAssistanceProfiles.SingleAsync();
        await Handler(first).Handle(Command(member, "helper", "cancel"), default);
        var reviewer = new ReviewTransferAssistanceProfileHandler(second, new AuditService(second),
            new ReviewTransferAssistanceProfileValidator(), NullLogger<ReviewTransferAssistanceProfileHandler>.Instance);
        var result = await reviewer.Handle(new(member.TenantId, Guid.NewGuid(), member.Id,
            new("helper", "verify", "Reviewed")), default);
        Assert.Equal("errors.transfer_assistance.concurrent_update", result.ErrorCode);
        await using var verify = TestDbFactory.Create(database);
        Assert.False((await verify.TransferAssistanceProfiles.SingleAsync()).HelperOptedIn);
    }

    [Fact]
    public async Task OtherTenantCannotChangeAccreditation()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedAsync(db);
        var command = Command(member, "helper", "request") with { TenantId = Guid.NewGuid() };
        var result = await Handler(db).Handle(command, default);
        Assert.Equal("errors.transfer_assistance.member_not_eligible", result.ErrorCode);
        Assert.Empty(db.TransferAssistanceProfiles);
    }

    private static async Task<Member> SeedAsync(AppDbContext db)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test", Slug = "test" };
        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Role = "member",
            Email = "member@example.test", EmailVerifiedAt = DateTime.UtcNow,
        };
        var member = new Member
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, User = user,
            Status = "active", MembershipNumber = "TEST-001",
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }
}
