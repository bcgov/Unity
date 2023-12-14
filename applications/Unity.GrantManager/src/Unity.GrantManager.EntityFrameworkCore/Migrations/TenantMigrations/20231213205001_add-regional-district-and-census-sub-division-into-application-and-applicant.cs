using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class addregionaldistrictandcensussubdivisionintoapplicationandapplicant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CensusSubdivision",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionalDistrict",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CensusSubdivision",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionalDistrict",
                table: "Applicants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CensusSubdivision",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RegionalDistrict",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CensusSubdivision",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "RegionalDistrict",
                table: "Applicants");
        }
    }
}
