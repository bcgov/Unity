using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddSiteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierSiteCode",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Country",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailAddress",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EFTAdvicePref",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteProtected",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedInCas",
                schema: "Payments",
                table: "Sites",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierSiteCode",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "EmailAddress",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "EFTAdvicePref",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteProtected",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LastUpdatedInCas",
                schema: "Payments",
                table: "Sites");
        }
    }
}
