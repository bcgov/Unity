using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateSiteSupplierDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalAddress",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ExpenseAuthorityName",
                schema: "Payments",
                table: "BatchPaymentRequests");

            migrationBuilder.RenameColumn(
                name: "IssuedByName",
                schema: "Payments",
                table: "BatchPaymentRequests",
                newName: "RequesterName");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingAddress",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingAddress",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "MailingAddress",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Province",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MailingAddress",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Province",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.RenameColumn(
                name: "RequesterName",
                schema: "Payments",
                table: "BatchPaymentRequests",
                newName: "IssuedByName");

            migrationBuilder.AddColumn<string>(
                name: "PhysicalAddress",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExpenseAuthorityName",
                schema: "Payments",
                table: "BatchPaymentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
