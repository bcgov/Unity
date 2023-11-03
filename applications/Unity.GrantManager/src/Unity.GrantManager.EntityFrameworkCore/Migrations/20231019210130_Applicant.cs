using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class Applicant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NonRegisteredBusinessName",
                table: "UnityApplicant",
                type: "character varying(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrgName",
                table: "UnityApplicant",
                type: "character varying(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrgNumber",
                table: "UnityApplicant",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrgStatus",
                table: "UnityApplicant",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "UnityApplicant",
                type: "character varying(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuplierNumber",
                table: "UnityApplicant",
                type: "character varying(50)",
                nullable: true);
                
            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "UnityApplicant",
                type: "character varying(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubSector",
                table: "UnityApplicant",
                type: "character varying(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UnityApplicant",
                type: "character varying(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproxNumberOfEmployees",
                table: "UnityApplicant",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomicRegion",
                table: "UnityApplicant",
                type: "character varying(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Community",
                table: "UnityApplicant",
                type: "character varying(500)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndigenousOrgInd",
                table: "UnityApplicant",
                type: "character varying(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectoralDistrict",
                table: "UnityApplicant",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NonRegisteredBusinessName",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "OrgName",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "OrgNumber",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "OrgStatus",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "SubSector",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "ApproxNumberOfEmployees",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "EconomicRegion",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "Community",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "EconomicRegion",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "IndigenousOrgInd",
                table: "UnityApplicant");

            migrationBuilder.DropColumn(
                name: "ElectoralDistrict",
                table: "UnityApplicant");
        }
    }
}
