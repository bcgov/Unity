using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_EmailLogs_SentDateTime_CreationTime_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CreationTime",
                schema: "Notifications",
                table: "EmailLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentDateTime",
                schema: "Notifications",
                table: "EmailLogs",
                column: "SentDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_CreationTime",
                schema: "Notifications",
                table: "EmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_SentDateTime",
                schema: "Notifications",
                table: "EmailLogs");
        }
    }
}
