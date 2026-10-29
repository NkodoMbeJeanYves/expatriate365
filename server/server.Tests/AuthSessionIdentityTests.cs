using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using server.Application.Auth.Commands;
using server.Application.Auth.DTOs;
using server.Application.Auth.Queries;
using server.Domain.Entities;
using server.Infrastructure.Services;

namespace server.Tests;

public class AuthSessionIdentityTests
{
    [Theory]
    [InlineData(false, false, "user")]
    [InlineData(true, false, "member")]
    [InlineData(true, true, "board_member")]
    public async Task LoginMeAndRefresh_ReturnConsistentIdentity(bool linked, bool onBoard, string expectedType)
    {
        await using var db = TestDbFactory.Create();
        var tenant = new Tenant { Id = Guid.NewGuid() };
        var password = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "member@example.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };
        var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        if (linked) db.Members.Add(member);
        if (onBoard) db.BoardMembers.Add(new BoardMember
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, MemberId = member.Id,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
        });
        await db.SaveChangesAsync();
        var jwt = new JwtService(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = new string('x', 64) }).Build());
        var permissions = new PermissionResolverService(db);
        var login = await new LoginCommandHandler(db, jwt, NullLogger<LoginCommandHandler>.Instance,
            new AuditService(db), permissions).Handle(new LoginCommand(new LoginRequest(user.Email, password)), default);
        Assert.True(login.IsSuccess);
        var me = await new GetMeQueryHandler(db, permissions, NullLogger<GetMeQueryHandler>.Instance)
            .Handle(new GetMeQuery(user.Id), default);
        var refresh = await new RefreshTokenCommandHandler(db, jwt,
            NullLogger<RefreshTokenCommandHandler>.Instance, permissions)
            .Handle(new RefreshTokenCommand(login.Data!.RefreshToken), default);
        Assert.True(refresh.IsSuccess);
        var expectedId = linked ? member.Id.ToString() : user.Id.ToString();
        Assert.Equal(expectedType, login.Data.User.EntityType);
        Assert.Equal(expectedType, me.Data!.EntityType);
        Assert.Equal(expectedType, refresh.Data!.User.EntityType);
        Assert.Equal(expectedId, login.Data.User.EntityId);
        Assert.Equal(expectedId, me.Data.EntityId);
        Assert.Equal(expectedId, refresh.Data.User.EntityId);
    }

    [Fact]
    public async Task Me_ReturnsLinkedMemberAndEffectiveTenantPermissions()
    {
        await using var db = TestDbFactory.Create();
        var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Role = "member" };
        var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, TenantId = user.TenantId.Value };
        var role = new Role { Id = Guid.NewGuid(), Name = "member", Permissions = "[\"events.read\"]" };
        db.Users.Add(user);
        db.Members.Add(member);
        db.Roles.Add(role);
        db.TenantRoles.Add(new TenantRole
        {
            Id = Guid.NewGuid(), TenantId = member.TenantId, RoleId = role.Id,
            Permissions = "[\"transfer_assistance.create\"]",
        });
        await db.SaveChangesAsync();

        var handler = new GetMeQueryHandler(db, new PermissionResolverService(db),
            NullLogger<GetMeQueryHandler>.Instance);
        var result = await handler.Handle(new GetMeQuery(user.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("member", result.Data!.EntityType);
        Assert.Equal(member.Id.ToString(), result.Data.EntityId);
        Assert.Equal(["transfer_assistance.create"], result.Data.Permissions);
    }

    [Fact]
    public async Task Me_DoesNotUseMemberOrTenantFromAnotherCommunity()
    {
        await using var db = TestDbFactory.Create();
        var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Role = "member" };
        var otherTenant = Guid.NewGuid();
        db.Users.Add(user);
        db.Members.Add(new Member { Id = Guid.NewGuid(), UserId = user.Id, TenantId = otherTenant });
        await db.SaveChangesAsync();
        var handler = new GetMeQueryHandler(db, new PermissionResolverService(db),
            NullLogger<GetMeQueryHandler>.Instance);

        var result = await handler.Handle(new GetMeQuery(user.Id, otherTenant), default);

        Assert.Equal("user", result.Data!.EntityType);
        Assert.Equal(user.TenantId.ToString(), result.Data.TenantId);
        Assert.Empty(result.Data.Permissions);
    }

    [Fact]
    public async Task Me_PreservesSuperAdminSelectedTenant()
    {
        await using var db = TestDbFactory.Create();
        var user = new User { Id = Guid.NewGuid(), Role = "super_admin" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var selectedTenant = Guid.NewGuid();
        var handler = new GetMeQueryHandler(db, new PermissionResolverService(db),
            NullLogger<GetMeQueryHandler>.Instance);

        var result = await handler.Handle(new GetMeQuery(user.Id, selectedTenant), default);

        Assert.Equal(selectedTenant.ToString(), result.Data!.TenantId);
        Assert.Equal("super_admin", result.Data.EntityType);
        Assert.Equal(user.Id.ToString(), result.Data.EntityId);
    }
}
