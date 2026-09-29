using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "board_members");

            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                table: "board_members",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "board_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    label = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_board_roles_Tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_board_members_role_id",
                table: "board_members",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_board_roles_tenant_id_name",
                table: "board_roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_board_members_board_roles_role_id",
                table: "board_members",
                column: "role_id",
                principalTable: "board_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_members_board_roles_role_id",
                table: "board_members");

            migrationBuilder.DropTable(
                name: "board_roles");

            migrationBuilder.DropIndex(
                name: "IX_board_members_role_id",
                table: "board_members");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "board_members");

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "board_members",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
