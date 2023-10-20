using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CleanGrowth",
                table: "UnityAssessment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EconomicImpact",
                table: "UnityAssessment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialAnalysis",
                table: "UnityAssessment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InclusiveGrowth",
                table: "UnityAssessment",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleanGrowth",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "EconomicImpact",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "FinancialAnalysis",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "InclusiveGrowth",
                table: "UnityAssessment");
        }
    }
}
