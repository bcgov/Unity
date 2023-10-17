using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationFormHeaderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "UnityApplicationForm",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProposalDate",
                table: "UnityApplication",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomicRegion",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "UnityApplication",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalProjectBudget",
                table: "UnityApplication",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "City",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "EconomicRegion",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "TotalProjectBudget",
                table: "UnityApplication");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProposalDate",
                table: "UnityApplication",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }
    }
}
