using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB32438_NotifiedStatusVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotifiedStatus",
                table: "ApplicationStatuses",
                type: "text",
                nullable: true);

            // Seed NotifiedStatus for existing records
            migrationBuilder.Sql(@"
                UPDATE ""ApplicationStatuses""
                SET ""NotifiedStatus"" = 'Approved'
                WHERE ""StatusCode"" = 'GRANT_APPROVED';
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ApplicationStatuses""
                SET ""NotifiedStatus"" = 'Declined'
                WHERE ""StatusCode"" = 'GRANT_NOT_APPROVED';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifiedStatus",
                table: "ApplicationStatuses");
        }
    }
}
