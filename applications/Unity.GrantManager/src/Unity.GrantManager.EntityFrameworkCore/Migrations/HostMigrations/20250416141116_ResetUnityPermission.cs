using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class ResetUnityPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM public.\"PermissionGrants\" WHERE \"Name\" = 'Unity.GrantManager.ApplicationManagement.Review.AssessmentResults.Update.UpdateFinalStateFields';");
            migrationBuilder.Sql($"DELETE FROM public.\"PermissionGrants\" WHERE \"Name\" = 'Unity.GrantManager.ApplicationManagement.Review.Approval.Update.UpdateFinalStateFields';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Added by permission grant data seeder
        }
    }
}
