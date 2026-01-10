using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddFsbApNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FsbApNotified",
                schema: "Payments",
                table: "PaymentRequests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FsbNotificationEmailLogId",
                schema: "Payments",
                table: "PaymentRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FsbNotificationSentDate",
                schema: "Payments",
                table: "PaymentRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentRequestIds",
                schema: "Notifications",
                table: "EmailLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_FsbNotificationEmailLogId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "FsbNotificationEmailLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_FsbNotificationEmailLogId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "FsbApNotified",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "FsbNotificationEmailLogId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "FsbNotificationSentDate",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaymentRequestIds",
                schema: "Notifications",
                table: "EmailLogs");
        }
    }
}
