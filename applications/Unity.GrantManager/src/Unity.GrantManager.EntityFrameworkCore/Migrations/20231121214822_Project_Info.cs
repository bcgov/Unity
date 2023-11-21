using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class ProjectInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Acquisition",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Community",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommunityPopulation",
                table: "UnityApplication",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Forestry",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForestryFocus",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PercentageTotalProjectBudget",
                table: "UnityApplication",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectEndDate",
                table: "UnityApplication",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProjectFundingTotal",
                table: "UnityApplication",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectStartDate",
                table: "UnityApplication",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acquisition",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "Community",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "CommunityPopulation",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "Forestry",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ForestryFocus",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "PercentageTotalProjectBudget",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ProjectEndDate",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ProjectFundingTotal",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ProjectStartDate",
                table: "UnityApplication");
        }
    }
}
