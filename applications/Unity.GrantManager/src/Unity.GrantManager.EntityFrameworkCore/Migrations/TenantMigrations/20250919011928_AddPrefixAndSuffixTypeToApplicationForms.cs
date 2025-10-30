using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddPrefixAndSuffixTypeToApplicationForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "ApplicationForms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuffixType",
                table: "ApplicationForms",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "SuffixType",
                table: "ApplicationForms");
        }
    }
}
