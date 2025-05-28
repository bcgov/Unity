using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddScoresheetReportingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportColumns",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportKeys",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportViewName",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportData",
                schema: "Flex",
                table: "ScoresheetInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportColumns",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportKeys",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportViewName",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportData",
                schema: "Flex",
                table: "ScoresheetInstances");
        }
    }
}
