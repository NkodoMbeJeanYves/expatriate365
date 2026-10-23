using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Tests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRANSFER_ASSISTANCE_TEST_MYSQL")))
            Skip = "Set TRANSFER_ASSISTANCE_TEST_MYSQL to an isolated MySQL 8.0.46 instance (without Database).";
    }
}

[Trait("Category", "MySqlIntegration")]
public sealed class TransferAssistanceMySqlTests
{
    private const string BeforeWorkflow = "20261019131509_AddTransferAssistanceBeneficiaries";
    private const string Workflow = "20261019191302_AddTransferAssistanceWorkflow";
    private const string Repair = "20261022060000_RepairTransferAssistanceOpenRequestKeys";
    private static readonly string[] OpenStatuses =
    [
        "draft", "submitted", "matching", "witness_pending", "meeting_scheduled",
        "transfer_in_progress", "transfer_sent", "disputed", "suspended"
    ];

    [MySqlFact]
    public async Task FreshDatabase_MigratesAndCanBeRedeployed_OnTheProductionMySqlConfiguration()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains(Repair, await db.Database.GetAppliedMigrationsAsync());
        await using var connection = new MySqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT VERSION(), @@SESSION.sql_mode, @@character_set_database, @@collation_database
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.StartsWith("8.0.46", reader.GetString(0));
        var modes = reader.GetString(1).Split(',').ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(modes.SetEquals(new[]
        {
            "ONLY_FULL_GROUP_BY", "STRICT_TRANS_TABLES", "NO_ZERO_IN_DATE", "NO_ZERO_DATE",
            "ERROR_FOR_DIVISION_BY_ZERO", "NO_ENGINE_SUBSTITUTION"
        }));
        Assert.Equal("utf8mb4", reader.GetString(2));
        Assert.Equal("utf8mb4_unicode_ci", reader.GetString(3));
    }

    [MySqlFact]
    public async Task HistoricalDatabase_KeysOnlyActiveOpenRequests_AndPreservesAllRows()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync(BeforeWorkflow);
        var expected = new Dictionary<Guid, string?>();
        foreach (var status in OpenStatuses)
        {
            var member = await SeedMemberAsync(db);
            var openId = await InsertHistoricalRequestAsync(db, member, status);
            expected.Add(openId, Key(member));
            foreach (var closedStatus in new[] { "completed", "expired", "rejected", "cancelled" })
                expected.Add(await InsertHistoricalRequestAsync(db, member, closedStatus), null);
            expected.Add(await InsertHistoricalRequestAsync(db, member, status, false), null);
        }
        var closedOnlyMember = await SeedMemberAsync(db);
        expected.Add(await InsertHistoricalRequestAsync(db, closedOnlyMember, "completed"), null);

        await db.Database.MigrateAsync();
        var actual = await db.TransferAssistanceRequests.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.OpenRequestKey);
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (id, key) in expected)
            Assert.Equal(key, actual[id]);
    }

    [MySqlFact]
    public async Task AmbiguousHistory_StopsBeforeWorkflowDdl_AndCanResumeAfterTracedResolution()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync(BeforeWorkflow);
        var member = await SeedMemberAsync(db);
        await InsertHistoricalRequestAsync(db, member, "draft");
        var duplicateId = await InsertHistoricalRequestAsync(db, member, "submitted");

        var error = await Assert.ThrowsAsync<MySqlException>(() => db.Database.MigrateAsync());
        Assert.Equal(1062, error.Number);
        Assert.Contains(Key(member), error.Message);
        Assert.DoesNotContain(Workflow, await db.Database.GetAppliedMigrationsAsync());
        await using (var connection = new MySqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*) FROM information_schema.columns
                WHERE table_schema = DATABASE() AND table_name = 'transfer_assistance_requests'
                  AND column_name = 'open_request_key'
                """;
            Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }

        // Production resolutions require a business decision and audit; this update only repairs the test fixture.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE transfer_assistance_requests SET status = 'cancelled' WHERE id = {duplicateId}");
        await db.Database.MigrateAsync();
        Assert.Equal(2, await db.TransferAssistanceRequests.CountAsync());
        Assert.Single(await db.TransferAssistanceRequests.Where(r => r.OpenRequestKey != null).ToListAsync());
    }

    [MySqlFact]
    public async Task ForwardRepair_FixesAlreadyMigratedKeys_WithoutChangingBusinessData()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync(Workflow);
        var member = await SeedMemberAsync(db);
        var openId = await InsertHistoricalRequestAsync(db, member, "matching");
        var closedId = await InsertHistoricalRequestAsync(db, member, "completed");
        var inactiveId = await InsertHistoricalRequestAsync(db, member, "draft", false);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE transfer_assistance_requests SET open_request_key = {Key(member)} WHERE id = {closedId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE transfer_assistance_requests SET open_request_key = 'obsolete-inactive-key' WHERE id = {inactiveId}");
        var before = await db.TransferAssistanceRequests.AsNoTracking()
            .Select(r => new { r.Id, r.Status, r.IsActive, r.AmountMur, r.CreatedAt }).OrderBy(r => r.Id).ToListAsync();

        await db.Database.MigrateAsync();
        var requests = await db.TransferAssistanceRequests.AsNoTracking().ToDictionaryAsync(r => r.Id);
        Assert.Equal(Key(member), requests[openId].OpenRequestKey);
        Assert.Null(requests[closedId].OpenRequestKey);
        Assert.Null(requests[inactiveId].OpenRequestKey);
        Assert.Equal(before, await db.TransferAssistanceRequests.AsNoTracking()
            .Select(r => new { r.Id, r.Status, r.IsActive, r.AmountMur, r.CreatedAt }).OrderBy(r => r.Id).ToListAsync());
    }

    [MySqlFact]
    public async Task ForwardRepair_RejectsAmbiguousAlreadyMigratedHistory_WithoutClearingKeys()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync(Workflow);
        var member = await SeedMemberAsync(db);
        var firstId = await InsertHistoricalRequestAsync(db, member, "draft");
        await InsertHistoricalRequestAsync(db, member, "matching");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE transfer_assistance_requests SET open_request_key = {Key(member)} WHERE id = {firstId}");

        var error = await Assert.ThrowsAsync<MySqlException>(() => db.Database.MigrateAsync());
        Assert.Equal(1062, error.Number);
        Assert.DoesNotContain(Repair, await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(Key(member),
            await db.TransferAssistanceRequests.Where(r => r.Id == firstId).Select(r => r.OpenRequestKey).SingleAsync());
    }

    [MySqlFact]
    public async Task MySql_EnforcesOpenRequestUniquenessAndWorkflowConcurrency()
    {
        await using var database = new TestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await db.Database.MigrateAsync();
        var member = await SeedMemberAsync(db);
        var requestId = await InsertHistoricalRequestAsync(db, member, "matching");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE transfer_assistance_requests SET open_request_key = {Key(member)} WHERE id = {requestId}");

        await using (var duplicateContext = database.CreateContext())
        {
            var duplicate = new TransferAssistanceRequest
            {
                Id = Guid.NewGuid(), TenantId = member.TenantId, RequesterMemberId = member.Id,
                OpenRequestKey = Key(member), AvailableFrom = new DateOnly(2026, 10, 22),
                AvailableTo = new DateOnly(2026, 10, 23)
            };
            duplicateContext.TransferAssistanceRequests.Add(duplicate);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
            Assert.Equal(1062, Assert.IsType<MySqlException>(error.InnerException).Number);
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = await firstContext.TransferAssistanceRequests.SingleAsync(r => r.Id == requestId);
        var second = await secondContext.TransferAssistanceRequests.SingleAsync(r => r.Id == requestId);
        first.WorkflowVersion++;
        first.MeetingArea = "First update";
        await firstContext.SaveChangesAsync();
        second.WorkflowVersion++;
        second.MeetingArea = "Stale update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
        Assert.Equal("First update", await db.TransferAssistanceRequests.AsNoTracking()
            .Where(r => r.Id == requestId).Select(r => r.MeetingArea).SingleAsync());
    }

    private static string Key(Member member) => $"{member.TenantId:N}:{member.Id:N}";

    private static async Task<Member> SeedMemberAsync(AppDbContext db)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Migration test", Slug = $"test-{Guid.NewGuid():N}" };
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = $"{Guid.NewGuid():N}@example.test" };
        var member = new Member
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id,
            MembershipNumber = "M-001", Status = "active", JoinedDate = new DateOnly(2026, 10, 22)
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    private static async Task<Guid> InsertHistoricalRequestAsync(
        AppDbContext db, Member member, string status, bool isActive = true)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO transfer_assistance_requests
                (id, tenant_id, requester_member_id, beneficiary_name, destination_country, amount_mur,
                 receive_currency, receive_method, purpose, beneficiary_relationship, available_from,
                 available_to, meeting_area, status, is_active, created_at)
            VALUES ({id}, {member.TenantId}, {member.Id}, 'Beneficiary', 'CM', 1000,
                    'XAF', 'mobile_money', 'Family support', 'Parent', '2026-10-22',
                    '2026-10-23', 'Ebene', {status}, {isActive}, '2026-10-22 00:00:00')
            """);
        return id;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string name = $"exp365_transfer_test_{Guid.NewGuid():N}";
        private readonly MySqlConnectionStringBuilder admin;
        private bool created;

        internal TestDatabase()
        {
            admin = new MySqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("TRANSFER_ASSISTANCE_TEST_MYSQL")
                ?? throw new InvalidOperationException("TRANSFER_ASSISTANCE_TEST_MYSQL is required."));
            if (!string.IsNullOrEmpty(admin.Database))
                throw new InvalidOperationException("The isolated test connection must not specify Database.");
        }

        internal string ConnectionString => new MySqlConnectionStringBuilder(admin.ConnectionString) { Database = name }.ConnectionString;

        internal async Task CreateAsync()
        {
            await using var connection = new MySqlConnection(admin.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE `{name}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await command.ExecuteNonQueryAsync();
            created = true;
        }

        internal AppDbContext CreateContext()
            => new(new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 0, 46))).Options);

        public async ValueTask DisposeAsync()
        {
            if (!created)
                return;
            await using var connection = new MySqlConnection(admin.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE `{name}`";
            await command.ExecuteNonQueryAsync();
        }
    }
}
