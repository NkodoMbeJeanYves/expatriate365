using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAssistanceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transfer_assistance_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    member_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    helper_opted_in = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    helper_verification_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    helper_reviewed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    helper_reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    helper_verification_reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    witness_opted_in = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    witness_verification_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    witness_reviewed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    witness_reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    witness_verification_reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_assistance_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_profiles_Members_member_id",
                        column: x => x.member_id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_profiles_member_id",
                table: "transfer_assistance_profiles",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_profiles_tenant_id_helper_verification_s~",
                table: "transfer_assistance_profiles",
                columns: new[] { "tenant_id", "helper_verification_status", "witness_verification_status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_profiles_tenant_id_member_id",
                table: "transfer_assistance_profiles",
                columns: new[] { "tenant_id", "member_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transfer_assistance_profiles");
        }
    }
}
