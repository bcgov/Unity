using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddApplicationFormAIConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomaticallyGenerateAIAnalysis",
                table: "ApplicationForms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManuallyInitiateAIAnalysis",
                table: "ApplicationForms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomaticallyGenerateAIAnalysis",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "ManuallyInitiateAIAnalysis",
                table: "ApplicationForms");
        }
    }
}
