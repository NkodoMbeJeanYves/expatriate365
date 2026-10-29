using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using server.Application.Common;
using server.Application.TransferAssistance;
using server.Application.TransferAssistance.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

public sealed class TransferAssistanceRulesTests
{
    private static readonly string Verify = $"[\"{Permissions.TransferAssistanceVerifyUsers}\"]";

    [Fact]
    public async Task Rules_mirror_configuration_and_clamp_windows()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedMemberAsync(db, "member");
        var options = new TransferAssistanceOptions
        {
            RequireMfa = true, MatchingWindowDays = 500, InvitationWindowDays = 0,
            MinimumRequestAmountMur = 100, MaximumRequestAmountMur = 5000
        };

        var rules = await Handler(db, options).Handle(new(member.TenantId, member.Id), default);

        Assert.True(rules.RequireMfa);
        Assert.Equal(90, rules.MatchingWindowDays);
        Assert.Equal(1, rules.InvitationWindowDays);
        Assert.Equal(100, rules.MinimumRequestAmount);
        Assert.Equal(5000, rules.MaximumRequestAmount);
        Assert.Equal("MUR", rules.AmountCurrency);
        Assert.True(rules.RequestsOpen);
    }

    [Fact]
    public async Task Zero_maximum_means_requests_are_closed()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedMemberAsync(db, "member");

        var rules = await Handler(db, new TransferAssistanceOptions { MaximumRequestAmountMur = 0 })
            .Handle(new(member.TenantId, member.Id), default);

        Assert.False(rules.RequestsOpen);
    }

    [Theory]
    [InlineData("member", true, false, false, true)]
    [InlineData("member", true, true, false, false)]
    [InlineData("member", true, true, true, true)]
    [InlineData("member", false, false, false, false)]
    [InlineData("org_admin", true, false, true, false)]
    public async Task Eligibility_reflects_account_state(
        string role, bool emailVerified, bool requireMfa, bool mfaEnabled, bool canRequest)
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedMemberAsync(db, role, emailVerified, mfaEnabled);

        var me = (await Handler(db, new TransferAssistanceOptions { RequireMfa = requireMfa })
            .Handle(new(member.TenantId, member.Id), default)).MyEligibility;

        Assert.True(me.MemberActive);
        Assert.Equal(emailVerified, me.EmailVerified);
        Assert.Equal(mfaEnabled, me.MfaEnabled);
        Assert.Equal(role == "org_admin", me.IsCommunityAdmin);
        Assert.Equal(canRequest, me.CanRequestAccreditation);
    }

    [Fact]
    public async Task Inactive_or_unknown_member_cannot_request()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedMemberAsync(db, "member");
        member.Status = "suspended";
        await db.SaveChangesAsync();
        var handler = Handler(db, new TransferAssistanceOptions { RequireMfa = false });

        var suspended = (await handler.Handle(new(member.TenantId, member.Id), default)).MyEligibility;
        var unknown = (await handler.Handle(new(member.TenantId, Guid.NewGuid()), default)).MyEligibility;

        Assert.False(suspended.MemberActive);
        Assert.False(suspended.CanRequestAccreditation);
        Assert.False(unknown.MemberActive);
        Assert.False(unknown.CanRequestAccreditation);
    }

    [Fact]
    public async Task Verifier_roles_honour_tenant_overrides_and_exclude_platform_admin()
    {
        await using var db = TestDbFactory.Create();
        var member = await SeedMemberAsync(db, "member");
        var otherTenant = Guid.NewGuid();
        var orgAdmin = Role("org_admin", Verify);
        var auditor = Role("auditor", Verify);
        var treasurer = Role("treasurer", "[]");
        var president = Role("president", "not-json");
        db.Roles.AddRange(Role("super_admin", Verify), orgAdmin, auditor, treasurer, president,
            Role("censor", Verify, isActive: false));
        db.TenantRoles.AddRange(
            new TenantRole { Id = Guid.NewGuid(), TenantId = member.TenantId, RoleId = auditor.Id, Permissions = "[]" },
            new TenantRole { Id = Guid.NewGuid(), TenantId = member.TenantId, RoleId = treasurer.Id, Permissions = Verify },
            new TenantRole { Id = Guid.NewGuid(), TenantId = otherTenant, RoleId = president.Id, Permissions = Verify });
        await db.SaveChangesAsync();

        var rules = await Handler(db, new TransferAssistanceOptions()).Handle(new(member.TenantId, member.Id), default);

        Assert.Equal(["org_admin", "treasurer"], rules.VerifierRoles);
    }

    private static GetTransferAssistanceRulesHandler Handler(AppDbContext db, TransferAssistanceOptions options) =>
        new(db, Options.Create(options), NullLogger<GetTransferAssistanceRulesHandler>.Instance);

    private static Role Role(string name, string permissions, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), Name = name, Label = name, Permissions = permissions, IsActive = isActive };

    private static async Task<Member> SeedMemberAsync(
        AppDbContext db, string role, bool emailVerified = true, bool mfaEnabled = false)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test", Slug = Guid.NewGuid().ToString("N") };
        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Role = role, Email = "rules@example.test",
            EmailVerifiedAt = emailVerified ? DateTime.UtcNow : null, MfaEnabled = mfaEnabled,
        };
        var member = new Member
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, User = user,
            Status = "active", MembershipNumber = "RULES-001",
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }
}
