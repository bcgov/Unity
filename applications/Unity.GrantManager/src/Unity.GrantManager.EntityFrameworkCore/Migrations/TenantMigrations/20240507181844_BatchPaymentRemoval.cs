using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class BatchPaymentRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseApprovals_BatchPaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_BatchPaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropTable(
                name: "BatchPaymentRequests",
                schema: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseApprovals_BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.DropColumn(
                name: "BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "PaymentRequests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationProvider",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequesterName",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionDate",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseApprovals_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "PaymentRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "PaymentRequestId",
                principalSchema: "Payments",
                principalTable: "PaymentRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseApprovals_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "CorrelationProvider",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "RequesterName",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "DecisionDate",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "BatchPaymentRequests",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequesterName = table.Column<string>(type: "text", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchPaymentRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "BatchPaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseApprovals_BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "BatchPaymentRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseApprovals_BatchPaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "BatchPaymentRequestId",
                principalSchema: "Payments",
                principalTable: "BatchPaymentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_BatchPaymentRequests_BatchPaymentRequestId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "BatchPaymentRequestId",
                principalSchema: "Payments",
                principalTable: "BatchPaymentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
