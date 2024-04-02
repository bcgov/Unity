using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteNumber",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.AddColumn<long>(
                name: "Number",
                schema: "Payments",
                table: "Suppliers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Number",
                schema: "Payments",
                table: "Sites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                schema: "Payments",
                table: "BatchPaymentRequests",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Number",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteId",
                schema: "Payments",
                table: "BatchPaymentRequests");

            migrationBuilder.AddColumn<string>(
                name: "SiteNumber",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
