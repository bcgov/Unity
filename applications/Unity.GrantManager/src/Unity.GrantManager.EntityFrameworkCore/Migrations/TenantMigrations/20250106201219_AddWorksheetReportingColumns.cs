using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddWorksheetReportingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportColumns",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportKeys",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportViewName",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportColumns",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.DropColumn(
                name: "ReportKeys",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.DropColumn(
                name: "ReportViewName",
                schema: "Flex",
                table: "Worksheets");
        }
    }
}
