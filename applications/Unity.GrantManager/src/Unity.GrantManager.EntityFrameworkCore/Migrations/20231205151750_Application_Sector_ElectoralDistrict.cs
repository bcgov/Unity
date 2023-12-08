using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationSectorElectoralDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ElectoralDistrict",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubSector",
                table: "UnityApplication",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectoralDistrict",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "SubSector",
                table: "UnityApplication");
        }
    }
}
