using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentIdPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentIdPrefix",
                table: "PaymentConfigurations",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                schema: "Payments",
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfigurations_PaymentIdPrefix",
                schema: "Payments",
                table: "PaymentConfigurations",
                column: "PaymentIdPrefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                schema: "Payments",
                name: "IX_PaymentConfigurations_PaymentIdPrefix",
                table: "PaymentConfigurations");

            migrationBuilder.DropColumn(
                schema: "Payments",
                name: "PaymentIdPrefix",
                table: "PaymentConfigurations");
        }
    }
}
