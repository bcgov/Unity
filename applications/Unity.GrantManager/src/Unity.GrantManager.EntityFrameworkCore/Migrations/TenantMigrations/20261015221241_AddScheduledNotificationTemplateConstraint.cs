using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddScheduledNotificationTemplateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "TemplateVariables",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Application",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "Applicant");

            migrationBuilder.Sql(
                @"DELETE FROM ""Notifications"".""ScheduledNotifications"" AS scheduled
                  WHERE NOT EXISTS (
                      SELECT 1
                      FROM ""Notifications"".""EmailTemplates"" AS template
                      WHERE template.""Id"" = scheduled.""EmailTemplateId"");");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_EmailTemplateId",
                schema: "Notifications",
                table: "ScheduledNotifications",
                column: "EmailTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledNotifications_EmailTemplates_EmailTemplateId",
                schema: "Notifications",
                table: "ScheduledNotifications",
                column: "EmailTemplateId",
                principalSchema: "Notifications",
                principalTable: "EmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledNotifications_EmailTemplates_EmailTemplateId",
                schema: "Notifications",
                table: "ScheduledNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledNotifications_EmailTemplateId",
                schema: "Notifications",
                table: "ScheduledNotifications");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "TemplateVariables",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Applicant",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "Application");
        }
    }
}
