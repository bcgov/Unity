using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicantImportColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<double>(
                name: "RedStop",
                table: "Applicants",
                type: "boolean",
                defaultValue: false,
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "SiteDefault",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PhoneExtension",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Phone2Extension",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                                name: "SiteDefault",
                                table: "Applicants");

            migrationBuilder.DropColumn(
                                name: "PhoneExtension",
                                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                                name: "Phone2Extension",
                                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                                name: "RedStop",
                                table: "Applicants");

        }
    }
}
