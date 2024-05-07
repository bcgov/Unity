using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class BatchPaymentRemovalRenaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "PaymentRequestId",
                principalSchema: "Payments",
                principalTable: "PaymentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "PaymentRequestId",
                principalSchema: "Payments",
                principalTable: "PaymentRequests",
                principalColumn: "Id");
        }
    }
}
