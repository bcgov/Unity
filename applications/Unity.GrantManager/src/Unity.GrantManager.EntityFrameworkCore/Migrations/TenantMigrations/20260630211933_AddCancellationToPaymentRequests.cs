using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddCancellationToPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                schema: "Payments",
                table: "PaymentRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledById",
                schema: "Payments",
                table: "PaymentRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledOn",
                schema: "Payments",
                table: "PaymentRequests",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {          

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "CancelledById",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "CancelledOn",
                schema: "Payments",
                table: "PaymentRequests");     
        }
    }
}
