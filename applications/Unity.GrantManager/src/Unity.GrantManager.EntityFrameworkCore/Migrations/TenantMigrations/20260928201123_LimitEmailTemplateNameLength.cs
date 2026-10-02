using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class LimitEmailTemplateNameLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Application",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "Applicant");

            // Truncate existing template names that exceed the new 50 character limit
            migrationBuilder.Sql(
                @"UPDATE ""Notifications"".""EmailTemplates""
                  SET ""Name"" = LEFT(""Name"", 50)
                  WHERE LENGTH(""Name"") > 50;");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Applicant",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "Application");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
