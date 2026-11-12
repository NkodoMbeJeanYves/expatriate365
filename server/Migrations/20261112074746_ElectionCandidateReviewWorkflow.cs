using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class ElectionCandidateReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_reason",
                table: "election_candidates",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "election_candidates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by",
                table: "election_candidates",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "election_candidates",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE election_candidates SET status = 'approved'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_reason",
                table: "election_candidates");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "election_candidates");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "election_candidates");

            migrationBuilder.DropColumn(
                name: "status",
                table: "election_candidates");
        }
    }
}
