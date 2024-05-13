using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentRequestAddColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "CasHttpStatusCode",
                schema: "Payments",
                table: "PaymentRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CasResponse",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CasHttpStatusCode",
                schema: "Payments",
                table: "PaymentRequests");

             migrationBuilder.DropColumn(
                name: "CasResponse",
                schema: "Payments",
                table: "PaymentRequests");
        }
    }
}
