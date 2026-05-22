using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddAuditHistoryStatusAndAuditorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditStatus",
                table: "AuditHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorName",
                table: "AuditHistories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "AuditHistories");

            migrationBuilder.DropColumn(
                name: "AuditorName",
                table: "AuditHistories");
        }
    }
}
