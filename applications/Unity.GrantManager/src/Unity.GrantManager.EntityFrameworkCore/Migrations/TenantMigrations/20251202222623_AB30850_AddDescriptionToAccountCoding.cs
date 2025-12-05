using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB30850_AddDescriptionToAccountCoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Payments",
                table: "AccountCodings",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Payments",
                table: "AccountCodings");
        }
    }
}
