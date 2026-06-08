using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ScheduledNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Notifications");

            migrationBuilder.CreateTable(
                name: "ScheduledNotifications",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerType = table.Column<string>(type: "text", maxLength: 64, nullable: false),
                    TriggerDetail = table.Column<string>(type: "text", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EventType = table.Column<string>(type: "text", maxLength: 128, nullable: true),
                    ApplicationStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationStatus = table.Column<string>(type: "text", maxLength: 128, nullable: true),
                    RecipientCategory = table.Column<string>(type: "text", nullable: true),
                    RecipientIdentifier = table.Column<string>(type: "text", nullable: true),
                    DateField = table.Column<string>(type: "text", maxLength: 128, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalSchema: "Notifications",
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_EmailTemplateId",
                schema: "Notifications",
                table: "ScheduledNotifications",
                column: "EmailTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScheduledNotifications", schema: "Notifications");
        }
    }
}
