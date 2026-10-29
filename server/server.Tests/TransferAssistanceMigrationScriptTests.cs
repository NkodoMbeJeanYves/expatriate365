using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using server.Infrastructure.Persistence;

namespace server.Tests;

public sealed class TransferAssistanceMigrationScriptTests
{
    [Fact]
    public void WorkflowScript_PreflightsBeforeDdl_AndOnlyKeysActiveOpenRows()
    {
        using var db = CreateContext();
        var script = db.GetService<IMigrator>().GenerateScript(
            "20261019131509_AddTransferAssistanceBeneficiaries",
            "20261019191302_AddTransferAssistanceWorkflow");

        var preflight = script.IndexOf("CREATE TEMPORARY TABLE transfer_assistance_open_request_preflight", StringComparison.Ordinal);
        var ddl = script.IndexOf("ADD `beneficiary_confirmation`", StringComparison.Ordinal);
        Assert.True(preflight >= 0 && ddl > preflight);
        Assert.Contains("WHERE is_active = 1 AND status IN", script);
        Assert.DoesNotContain("INNER JOIN", script);
        Assert.Contains("CREATE UNIQUE INDEX `IX_transfer_assistance_requests_open_request_key`", script);
    }

    [Fact]
    public void ForwardScript_RepairsAppliedWorkflow_WithoutSchemaChanges()
    {
        using var db = CreateContext();
        var script = db.GetService<IMigrator>().GenerateScript(
            "20261019191302_AddTransferAssistanceWorkflow",
            "20261022060000_RepairTransferAssistanceOpenRequestKeys");

        Assert.Contains("CREATE TEMPORARY TABLE transfer_assistance_open_request_preflight", script);
        Assert.Contains("SET open_request_key = NULL", script);
        Assert.Contains("WHERE is_active = 1 AND status IN", script);
        Assert.DoesNotContain("ALTER TABLE", script);
    }

    [Fact]
    public void CurrentModel_MatchesTheMigrationSnapshot()
    {
        using var db = CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void FullDeploymentScript_CanBeGeneratedIdempotently()
    {
        using var db = CreateContext();
        var script = db.GetService<IMigrator>().GenerateScript(
            options: MigrationsSqlGenerationOptions.Idempotent);

        Assert.Contains("__EFMigrationsHistory", script);
        Assert.Contains("20261022060000_RepairTransferAssistanceOpenRequestKeys", script);
        Assert.Contains("CREATE TEMPORARY TABLE transfer_assistance_open_request_preflight", script);
    }

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=127.0.0.1;Database=script_generation_only;User ID=unused;",
                new MySqlServerVersion(new Version(8, 0, 46))).Options);
}
