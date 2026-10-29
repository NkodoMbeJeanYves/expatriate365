using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAssistanceBeneficiaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "beneficiary_member_id",
                table: "transfer_assistance_requests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_beneficiary_member_id",
                table: "transfer_assistance_requests",
                column: "beneficiary_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_assistance_requests_tenant_id_beneficiary_member_id",
                table: "transfer_assistance_requests",
                columns: new[] { "tenant_id", "beneficiary_member_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_transfer_assistance_requests_Members_beneficiary_member_id",
                table: "transfer_assistance_requests",
                column: "beneficiary_member_id",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transfer_assistance_requests_Members_beneficiary_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_beneficiary_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropIndex(
                name: "IX_transfer_assistance_requests_tenant_id_beneficiary_member_id",
                table: "transfer_assistance_requests");

            migrationBuilder.DropColumn(
                name: "beneficiary_member_id",
                table: "transfer_assistance_requests");
        }
    }
}
