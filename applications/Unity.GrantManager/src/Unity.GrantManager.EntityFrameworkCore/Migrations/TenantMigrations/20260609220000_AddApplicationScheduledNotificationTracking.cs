using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddApplicationScheduledNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationScheduledNotificationTracking",
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
                    table.PrimaryKey("PK_ApplicationScheduledNotificationTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationScheduledNotificationTracking_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationScheduledNotificationTracking_ScheduledNotifications_ScheduledNotificationId",
                        column: x => x.ScheduledNotificationId,
                        principalSchema: "Notifications",
                        principalTable: "ScheduledNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for efficient lookups
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationScheduledNotificationTracking_ApplicationId_ScheduledNotificationId_DateField",
                schema: "Notifications",
                table: "ApplicationScheduledNotificationTracking",
                columns: new[] { "ApplicationId", "ScheduledNotificationId", "DateField" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationScheduledNotificationTracking_ScheduledNotificationId",
                schema: "Notifications",
                table: "ApplicationScheduledNotificationTracking",
                column: "ScheduledNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationScheduledNotificationTracking_CreationTime",
                schema: "Notifications",
                table: "ApplicationScheduledNotificationTracking",
                column: "CreationTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationScheduledNotificationTracking",
                schema: "Notifications");
        }
    }
}
