using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleIsCustomized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "permissions",
                table: "roles",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000,
                oldDefaultValue: "[]")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_customized",
                table: "roles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_customized",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "permissions",
                table: "roles",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "[]")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
