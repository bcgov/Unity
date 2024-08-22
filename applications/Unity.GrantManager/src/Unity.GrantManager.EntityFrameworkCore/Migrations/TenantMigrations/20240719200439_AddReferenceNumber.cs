using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddReferenceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: false,
                defaultValueSql: "random()");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ReferenceNumber",
                schema: "Payments",
                table: "PaymentRequests",
                column: "ReferenceNumber",
                unique: true);

                migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "PaymentRequests",
                schema: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_ReferenceNumber",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                schema: "Payments",
                table: "PaymentRequests");
        }
    }
}
