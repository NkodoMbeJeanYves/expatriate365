using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using server.Infrastructure.Persistence;

namespace server.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261022060000_RepairTransferAssistanceOpenRequestKeys")]
public sealed class RepairTransferAssistanceOpenRequestKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(TransferAssistanceOpenRequestKeySql.Preflight);
        migrationBuilder.Sql(TransferAssistanceOpenRequestKeySql.Backfill);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException(
            "Historical open request keys cannot be restored safely. Restore the verified pre-deployment backup instead.");
}
