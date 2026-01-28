using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Rename_ChseHttpStatusCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChseHttpStatusCode",
                schema: "Notifications",
                table: "EmailLogs",
                newName: "ChesHttpStatusCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChesHttpStatusCode",
                schema: "Notifications",
                table: "EmailLogs",
                newName: "ChseHttpStatusCode");
        }
    }
}
