using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentRequestsAddSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                schema: "Payments",
                table: "PaymentRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                schema: "Payments",
                name: "IX_PaymentRequests_SiteId",
                table: "PaymentRequests",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                schema: "Payments",
                name: "FK_Payments_Sites_SiteId",
                table: "PaymentRequests",
                column: "SiteId",
                principalSchema: "Payments",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "PaymentGroup",
                schema: "Payments",
                table: "BatchPaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaymentGroup",
                schema: "Payments",
                table: "PaymentRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGroup",
                schema: "Payments",
                table: "PaymentRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGroup",
                schema: "Payments",
                table: "BatchPaymentRequest",
                type: "integer",
                nullable: true);

        }
    }
}
