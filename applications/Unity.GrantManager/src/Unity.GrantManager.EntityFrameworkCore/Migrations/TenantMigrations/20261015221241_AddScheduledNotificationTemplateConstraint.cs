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

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_ScheduledNotifications_EmailTemplateId""
                  ON ""Notifications"".""ScheduledNotifications"" (""EmailTemplateId"");");

            migrationBuilder.Sql(
                @"DO $$
                  BEGIN
                      IF NOT EXISTS (
                          SELECT 1 FROM pg_constraint
                          WHERE conname = 'FK_ScheduledNotifications_EmailTemplates_EmailTemplateId'
                            AND conrelid = '""Notifications"".""ScheduledNotifications""'::regclass) THEN
                          ALTER TABLE ""Notifications"".""ScheduledNotifications""
                              ADD CONSTRAINT ""FK_ScheduledNotifications_EmailTemplates_EmailTemplateId""
                              FOREIGN KEY (""EmailTemplateId"")
                              REFERENCES ""Notifications"".""EmailTemplates"" (""Id"")
                              ON DELETE RESTRICT;
                      END IF;
                  END $$;");
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
