using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateSiteSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "Suppliers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                schema: "Payments",
                table: "Suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                schema: "Payments",
                table: "Suppliers");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Payments",
                table: "Sites",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
