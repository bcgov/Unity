using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_PaymentRequests_Performance_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CorrelationId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CreationTime",
                schema: "Payments",
                table: "PaymentRequests",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_Status",
                schema: "Payments",
                table: "PaymentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_TenantId_CreationTime",
                schema: "Payments",
                table: "PaymentRequests",
                columns: new[] { "TenantId", "CreationTime" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_CorrelationId",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_CreationTime",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_Status",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_TenantId_CreationTime",
                schema: "Payments",
                table: "PaymentRequests");
        }
    }
}
