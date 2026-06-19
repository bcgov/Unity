using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddScheduledNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledNotificationTracking",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledNotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateField = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NotificationSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotificationTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledNotificationTracking_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledNotificationTracking_ScheduledNotifications_ScheduledNotificationId",
                        column: x => x.ScheduledNotificationId,
                        principalSchema: "Notifications",
                        principalTable: "ScheduledNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for efficient lookups
            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_ApplicationId_ScheduledNotificationId_DateField",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                columns: new[] { "ApplicationId", "ScheduledNotificationId", "DateField" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_ScheduledNotificationId",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                column: "ScheduledNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_CreationTime",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                column: "CreationTime");

            // Fix ChesMsgId column type: convert from text to uuid (Guid)
            // Drop the existing ChesMsgId column (defined as text in the original migration)
            migrationBuilder.DropColumn(
                name: "ChesMsgId",
                schema: "Notifications",
                table: "EmailLogs");

            // Recreate it as a uuid column (nullable Guid)
            migrationBuilder.AddColumn<Guid>(
                name: "ChesMsgId",
                schema: "Notifications",
                table: "EmailLogs",
                type: "uuid",
                nullable: true);

            // OPTIMIZATION: Add indexes for DateBasedScheduledNotificationJob performance
            // These indexes support batch queries and reduce N+1 query patterns
            
            // Index for ScheduledNotification queries filtering by IsActive and DateField
            migrationBuilder.CreateIndex(
                name: "idx_scheduled_notifications_active_datefield",
                schema: "Notifications",
                table: "ScheduledNotifications",
                columns: new[] { "IsActive", "DateField" });

            // Index for Application queries filtering by ApplicationFormId (primary filter in job)
            migrationBuilder.CreateIndex(
                name: "idx_applications_formid",
                table: "Applications",
                column: "ApplicationFormId");

            // Composite index for Application queries with DueDate filtering
            migrationBuilder.CreateIndex(
                name: "idx_applications_formid_duedate",
                table: "Applications",
                columns: new[] { "ApplicationFormId", "DueDate" });

            // Index for ApplicantAgent queries filtering by ApplicationId
            migrationBuilder.CreateIndex(
                name: "idx_applicant_agents_applicationid",
                table: "ApplicantAgents",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert ChesMsgId column type: convert from uuid back to text
            migrationBuilder.DropColumn(
                name: "ChesMsgId",
                schema: "Notifications",
                table: "EmailLogs");

            migrationBuilder.AddColumn<string>(
                name: "ChesMsgId",
                schema: "Notifications",
                table: "EmailLogs",
                type: "text",
                nullable: true);

            // Drop optimized indexes
            migrationBuilder.DropIndex(
                name: "idx_scheduled_notifications_active_datefield",
                schema: "Notifications",
                table: "ScheduledNotifications");

            migrationBuilder.DropIndex(
                name: "idx_applications_formid",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "idx_applications_formid_duedate",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "idx_applicant_agents_applicationid",
                table: "ApplicantAgents");

            // Drop tracking table
            migrationBuilder.DropTable(
                name: "ScheduledNotificationTracking",
                schema: "Notifications");
        }
    }
}
