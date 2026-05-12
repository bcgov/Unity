using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class RemoveRenderedHtmlAndRenderFormIoToHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenderedHTML",
                table: "ApplicationFormSubmissions");

            migrationBuilder.DropColumn(
                name: "RenderFormIoToHtml",
                table: "ApplicationForms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RenderedHTML",
                table: "ApplicationFormSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RenderFormIoToHtml",
                table: "ApplicationForms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
