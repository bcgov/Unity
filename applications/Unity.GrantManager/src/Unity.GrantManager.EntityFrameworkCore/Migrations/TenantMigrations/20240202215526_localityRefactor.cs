using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class localityRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CensusSubdivision",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "Community",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "EconomicRegion",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "ElectoralDistrict",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "RegionalDistrict",
                table: "Applicants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CensusSubdivision",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Community",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomicRegion",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectoralDistrict",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionalDistrict",
                table: "Applicants",
                type: "text",
                nullable: true);
        }
    }
}
