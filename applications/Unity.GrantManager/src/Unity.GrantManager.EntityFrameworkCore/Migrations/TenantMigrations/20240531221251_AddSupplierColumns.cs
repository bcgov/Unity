using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddSupplierColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Subcategory",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SIN",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessNumber",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierProtected",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandardIndustryClassification",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedInCAS",
                schema: "Payments",
                table: "Suppliers",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subcategory",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SIN",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "BusinessNumber",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SupplierProtected",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "StandardIndustryClassification",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LastUpdatedInCAS",
                schema: "Payments",
                table: "Suppliers");
        }
    }
}
