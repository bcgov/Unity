using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddIsDuplicatedToApplicants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IsDuplicated",
                table: "Applicants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDuplicated",
                table: "Applicants");
        }
    }
}
