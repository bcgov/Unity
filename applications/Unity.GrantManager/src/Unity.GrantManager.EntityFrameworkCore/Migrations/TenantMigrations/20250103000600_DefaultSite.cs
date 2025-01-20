using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class DefaultSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "PaymentRequests",
                schema: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                 name: "SiteId",
                 table: "Applicants",
                 type: "uuid",
                 nullable: true);

             migrationBuilder.DropColumn(
                                  name: "SiteDefault",
                                  table: "Applicants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                 name: "SiteId",
                 table: "Applicants");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "PaymentRequests",
                schema: "Payments");
        }
    }
}
