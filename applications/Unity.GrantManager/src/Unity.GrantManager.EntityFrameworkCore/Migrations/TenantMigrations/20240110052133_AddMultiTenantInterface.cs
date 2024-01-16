using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantInterface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Persons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Intakes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AssessmentComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AssessmentAttachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationStatuses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationFormVersion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationFormSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationForms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationAttachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Applicants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicantAgents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Addresses",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Intakes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssessmentComments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssessmentAttachments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationStatuses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationFormSubmissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationComments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationAssignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Addresses");
        }
    }
}
