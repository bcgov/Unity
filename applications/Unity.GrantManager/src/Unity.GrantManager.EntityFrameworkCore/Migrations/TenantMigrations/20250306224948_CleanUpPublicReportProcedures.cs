using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class CleanUpPublicReportProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cleanup and report generations scripts left in the public schema            
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS generate_submissions_view(UUID)");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS generate_worksheets_view(UUID)");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS generate_scoresheets_view(UUID)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
