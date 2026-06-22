using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddEmailLogRecipientAndEmailType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailType",
                schema: "Notifications",
                table: "EmailLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recipient",
                schema: "Notifications",
                table: "EmailLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailType",
                schema: "Notifications",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "Recipient",
                schema: "Notifications",
                table: "EmailLogs");
        }
    }
}
