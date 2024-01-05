using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class RemoveContactInfoFieldsFromApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactBusinessPhone",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ContactCellPhone",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ContactFullName",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ContactTitle",
                table: "Applications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactBusinessPhone",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactCellPhone",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactFullName",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactTitle",
                table: "Applications",
                type: "text",
                nullable: true);
        }
    }
}
