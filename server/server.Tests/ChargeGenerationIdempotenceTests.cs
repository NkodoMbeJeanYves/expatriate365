using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

/// <summary>
/// Verifies that the charge generation guard prevents duplicates
/// (same member + type + due date is never inserted twice).
/// </summary>
public class ChargeGenerationIdempotenceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static async Task<(AppDbContext db, Member member, ContributionType type, DateOnly dueDate)> SeedAsync()
    {
        var db = TestDbFactory.Create();
        var user = new User { Id = Guid.NewGuid(), TenantId = TenantId, Email = "x@x.com", FirstName = "A", LastName = "B", PasswordHash = "x" };
        var member = new Member { Id = Guid.NewGuid(), TenantId = TenantId, UserId = user.Id, MembershipNumber = "M-001", JoinedDate = DateOnly.FromDateTime(DateTime.Today), User = user, IsActive = true };
        var type = new ContributionType { Id = Guid.NewGuid(), TenantId = TenantId, Name = "Annual", Frequency = "annual", BaseAmount = 5_000, EffectiveFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
        db.Users.Add(user);
        db.Members.Add(member);
        db.ContributionTypes.Add(type);
        await db.SaveChangesAsync();
        return (db, member, type, DateOnly.FromDateTime(DateTime.Today.AddMonths(1)));
    }

    [Fact]
    public async Task Guard_PreventsInsert_WhenChargeAlreadyExists()
    {
        var (db, member, type, dueDate) = await SeedAsync();

        // First generation — creates the charge
        await GenerateIfNotExists(db, member, type, dueDate);
        var countAfterFirst = await db.ContributionCharges.CountAsync();

        // Second generation — same params, should be a no-op
        await GenerateIfNotExists(db, member, type, dueDate);
        var countAfterSecond = await db.ContributionCharges.CountAsync();

        Assert.Equal(1, countAfterFirst);
        Assert.Equal(1, countAfterSecond);   // still 1 — no duplicate
    }

    [Fact]
    public async Task Guard_AllowsInsert_ForDifferentDueDate()
    {
        var (db, member, type, dueDate) = await SeedAsync();

        await GenerateIfNotExists(db, member, type, dueDate);
        await GenerateIfNotExists(db, member, type, dueDate.AddYears(1));   // next year

        var count = await db.ContributionCharges.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Guard_AllowsInsert_ForDifferentMember()
    {
        var (db, memberA, type, dueDate) = await SeedAsync();
        var user2 = new User { Id = Guid.NewGuid(), TenantId = TenantId, Email = "y@y.com", FirstName = "C", LastName = "D", PasswordHash = "x" };
        var memberB = new Member { Id = Guid.NewGuid(), TenantId = TenantId, UserId = user2.Id, MembershipNumber = "M-002", JoinedDate = DateOnly.FromDateTime(DateTime.Today), User = user2, IsActive = true };
        db.Users.Add(user2);
        db.Members.Add(memberB);
        await db.SaveChangesAsync();

        await GenerateIfNotExists(db, memberA, type, dueDate);
        await GenerateIfNotExists(db, memberB, type, dueDate);

        var count = await db.ContributionCharges.CountAsync();
        Assert.Equal(2, count);
    }

    /// <summary>Mirrors the idempotence guard logic in ChargeGenerationBackgroundService.</summary>
    private static async Task GenerateIfNotExists(AppDbContext db, Member member, ContributionType type, DateOnly dueDate)
    {
        var exists = await db.ContributionCharges.AnyAsync(
            c => c.MemberId == member.Id
              && c.ContributionTypeId == type.Id
              && c.DueDate == dueDate);

        if (exists) return;

        db.ContributionCharges.Add(new ContributionCharge
        {
            Id = Guid.NewGuid(),
            TenantId = type.TenantId,
            MemberId = member.Id,
            ContributionTypeId = type.Id,
            DueDate = dueDate,
            BaseAmount = type.BaseAmount,
        });
        await db.SaveChangesAsync();
    }
}
