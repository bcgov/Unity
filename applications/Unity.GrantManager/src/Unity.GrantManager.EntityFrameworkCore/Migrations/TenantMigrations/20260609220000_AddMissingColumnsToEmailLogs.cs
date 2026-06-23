using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToEmailLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add ScheduledNotificationId - other columns were added in previous migrations:
            // - Status: migration 20241220224908_EmailStatus
            // - TemplateName: migration 20250424174914_add_email_template_name  
            // - PaymentRequestIds: migration 20260110002908_AddFsbApNotificationTracking
            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledNotificationId",
                schema: "Notifications",
                table: "EmailLogs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledNotificationId",
                schema: "Notifications",
                table: "EmailLogs");
        }
    }
}

