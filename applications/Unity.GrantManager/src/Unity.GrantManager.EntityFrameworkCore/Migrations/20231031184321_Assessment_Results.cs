using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EligibleAmount",
                table: "UnityApplication");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "UnityApplication",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssessmentResultDate",
                table: "UnityApplication",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentResultStatus",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclineRational",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DueDilligenceStatus",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LikelihoodOfFunding",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectSummary",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecommendedAmount",
                table: "UnityApplication",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalScore",
                table: "UnityApplication",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "AssessmentResultDate",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "AssessmentResultStatus",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "DeclineRational",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "DueDilligenceStatus",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "LikelihoodOfFunding",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ProjectSummary",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "RecommendedAmount",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "UnityApplication");

            migrationBuilder.AddColumn<double>(
                name: "EligibleAmount",
                table: "UnityApplication",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
