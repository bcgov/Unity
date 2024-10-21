using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicantAddColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "Applicants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnityApplicantId",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedOperatingDate",
                table: "Applicants",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiscalDay",
                table: "Applicants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiscalMonth",
                table: "Applicants",
                type: "character varying(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrgbookValidated",
                table: "Applicants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessNumber",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BceidBusinessGuid",
                table: "ApplicantAgents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BceidBusinessName",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BceidUserGuid",
                table: "ApplicantAgents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BceidUserName",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityEmail",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityName",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityProvider",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "StartedOperatingDate",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "FiscalYear",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "FiscalMonth",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "OrgbookValidated",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "UnityApplicantId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "BusinessNumber",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "BceidBusinessGuid",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "BceidBusinessName",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "BceidUserGuid",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "BceidUserName",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "IdentityEmail",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "IdentityName",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "IdentityProvider",
                table: "ApplicantAgents");
        }
    }
}
