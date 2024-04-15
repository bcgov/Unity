using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateSiteInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MailingAddress",
                schema: "Payments",
                table: "Sites",
                newName: "AddressLine3");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                schema: "Payments",
                table: "Sites");

            migrationBuilder.RenameColumn(
                name: "AddressLine3",
                schema: "Payments",
                table: "Sites",
                newName: "MailingAddress");
        }
    }
}
