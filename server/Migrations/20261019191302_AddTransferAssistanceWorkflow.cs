using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAssistanceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "beneficiary_confirmation",
                table: "transfer_assistance_requests",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "beneficiary_confirmed_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dispute_opened_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dispute_opened_by",
                table: "transfer_assistance_requests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "dispute_reason",
                table: "transfer_assistance_requests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "dispute_resolution",
                table: "transfer_assistance_requests",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "dispute_resolved_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dispute_resolved_by",
                table: "transfer_assistance_requests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handover_confirmed_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "helper_handover_confirmed",
                table: "transfer_assistance_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "helper_member_id",
                table: "transfer_assistance_requests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "meeting_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "open_request_key",
                table: "transfer_assistance_requests",
                type: "varchar(65)",
                maxLength: 65,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "requester_handover_confirmed",
                table: "transfer_assistance_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "suspended_from_status",
                table: "transfer_assistance_requests",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "transfer_evidence_reference",
                table: "transfer_assistance_requests",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "transfer_provider",
                table: "transfer_assistance_requests",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "transfer_sent_at",
                table: "transfer_assistance_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "witness_attested",
                table: "transfer_assistance_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "witness_member_id",
                table: "transfer_assistance_requests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<long>(
                name: "workflow_version",
                table: "transfer_assistance_requests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "transfer_assistance_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    request_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    event_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_assistance_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_events_Users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_events_transfer_assistance_requests_requ~",
                        column: x => x.request_id,
                        principalTable: "transfer_assistance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "transfer_assistance_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    request_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    helper_member_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    availability = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_assistance_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_offers_Members_helper_member_id",
                        column: x => x.helper_member_id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_offers_transfer_assistance_requests_requ~",
                        column: x => x.request_id,
                        principalTable: "transfer_assistance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "transfer_assistance_witness_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    request_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    witness_member_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    invited_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conflict_declared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    conflict_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    responded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_assistance_witness_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_witness_invitations_Members_witness_memb~",
                        column: x => x.witness_member_id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_witness_invitations_Users_invited_by_use~",
                        column: x => x.invited_by_user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_assistance_witness_invitations_transfer_assistance_~",
                        column: x => x.request_id,
                        principalTable: "transfer_assistance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE transfer_assistance_requests AS request_row
                INNER JOIN (
                    SELECT tenant_id, requester_member_id
                    FROM transfer_assistance_requests
                    WHERE status IN (
                        'draft', 'submitted', 'matching', 'witness_pending',
                        'meeting_scheduled', 'transfer_in_progress', 'transfer_sent',
                        'disputed', 'suspended'
                    )
                    GROUP BY tenant_id, requester_member_id
                    HAVING COUNT(*) = 1
                ) AS unique_open_request
                    ON unique_open_request.tenant_id = request_row.tenant_id
                    AND unique_open_request.requester_member_id = request_row.requester_member_id
                SET request_row.open_request_key = CONCAT(
                    REPLACE(LOWER(CAST(request_row.tenant_id AS CHAR)), '-', ''),
                    ':',
                    REPLACE(LOWER(CAST(request_row.requester_member_id AS CHAR)), '-', '')
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_helper_member_id",
                table: "transfer_assistance_requests",
                column: "helper_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_open_request_key",
                table: "transfer_assistance_requests",
                column: "open_request_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_tenant_id_helper_member_id_stat~",
                table: "transfer_assistance_requests",
                columns: new[] { "tenant_id", "helper_member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_tenant_id_status_expires_at",
                table: "transfer_assistance_requests",
                columns: new[] { "tenant_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_tenant_id_witness_member_id_sta~",
                table: "transfer_assistance_requests",
                columns: new[] { "tenant_id", "witness_member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_witness_member_id",
                table: "transfer_assistance_requests",
                column: "witness_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_events_actor_user_id",
                table: "transfer_assistance_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_events_request_id",
                table: "transfer_assistance_events",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_events_tenant_id_request_id_created_at",
                table: "transfer_assistance_events",
                columns: new[] { "tenant_id", "request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_offers_helper_member_id",
                table: "transfer_assistance_offers",
                column: "helper_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_offers_request_id_helper_member_id",
                table: "transfer_assistance_offers",
                columns: new[] { "request_id", "helper_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_offers_tenant_id_request_id_status",
                table: "transfer_assistance_offers",
                columns: new[] { "tenant_id", "request_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_witness_invitations_invited_by_user_id",
                table: "transfer_assistance_witness_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_witness_invitations_request_id",
                table: "transfer_assistance_witness_invitations",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_witness_invitations_tenant_id_request_id~",
                table: "transfer_assistance_witness_invitations",
                columns: new[] { "tenant_id", "request_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_witness_invitations_tenant_id_witness_me~",
                table: "transfer_assistance_witness_invitations",
                columns: new[] { "tenant_id", "witness_member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_witness_invitations_witness_member_id",
                table: "transfer_assistance_witness_invitations",
                column: "witness_member_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transfer_assistance_requests_Members_helper_member_id",
                table: "transfer_assistance_requests",
                column: "helper_member_id",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_transfer_assistance_requests_Members_witness_member_id",
                table: "transfer_assistance_requests",
                column: "witness_member_id",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transfer_assistance_requests_Members_helper_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_transfer_assistance_requests_Members_witness_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropTable(
                name: "transfer_assistance_events");

            migrationBuilder.DropTable(
                name: "transfer_assistance_offers");

            migrationBuilder.DropTable(
                name: "transfer_assistance_witness_invitations");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_helper_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_open_request_key",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_tenant_id_helper_member_id_stat~",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_tenant_id_status_expires_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_tenant_id_witness_member_id_sta~",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_witness_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "beneficiary_confirmation",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "beneficiary_confirmed_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_opened_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_opened_by",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_reason",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_resolution",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_resolved_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "dispute_resolved_by",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "handover_confirmed_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "helper_handover_confirmed",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "helper_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "meeting_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "open_request_key",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "requester_handover_confirmed",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "suspended_from_status",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "transfer_evidence_reference",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "transfer_provider",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "transfer_sent_at",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "witness_attested",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "witness_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "workflow_version",
                table: "transfer_assistance_requests");
        }
    }
}
