using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddTemplateTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_EmailAddressConfigurations_TenantId_IsDefault",
                schema: "Notifications",
                table: "EmailAddressConfigurations",
                newName: "IX_EmailAddressConfigurations_TenantId");

            migrationBuilder.AddColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "TemplateVariables",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Applicant");

            migrationBuilder.AddColumn<string>(
                name: "TemplateType",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Applicant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateType",
                schema: "Notifications",
                table: "TemplateVariables");

            migrationBuilder.DropColumn(
                name: "TemplateType",
                schema: "Notifications",
                table: "EmailTemplates");

            migrationBuilder.RenameIndex(
                name: "IX_EmailAddressConfigurations_TenantId",
                schema: "Notifications",
                table: "EmailAddressConfigurations",
                newName: "IX_EmailAddressConfigurations_TenantId_IsDefault");
        }
    }
}
