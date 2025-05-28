using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class add_template_variables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                schema: "Notifications",
                table: "TemplateVariables");

            migrationBuilder.RenameColumn(
                name: "InternalName",
                schema: "Notifications",
                table: "TemplateVariables",
                newName: "Token");

            migrationBuilder.AddColumn<string>(
                name: "MapTo",
                schema: "Notifications",
                table: "TemplateVariables",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapTo",
                schema: "Notifications",
                table: "TemplateVariables");

            migrationBuilder.RenameColumn(
                name: "Token",
                schema: "Notifications",
                table: "TemplateVariables",
                newName: "InternalName");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                schema: "Notifications",
                table: "TemplateVariables",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
