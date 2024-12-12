using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Redstop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RedStop",
                table: "Applicants",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldType: "boolean",
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RedStop",
                table: "Applicants",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldType: "boolean",
                oldNullable: true);
        }

    }
}
