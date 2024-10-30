using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AssessmentWorkflowPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace SendToTeamLead with Confirm
            migrationBuilder.Sql($"DELETE FROM public.\"PermissionGrants\" WHERE \"Name\"='GrantApplicationManagement.Assessments.Confirm';");
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\"='GrantApplicationManagement.Assessments.Confirm' WHERE \"Name\"='GrantApplicationManagement.Assessments.SendToTeamLead';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\"='GrantApplicationManagement.Assessments.SendToTeamLead' WHERE \"Name\"='GrantApplicationManagement.Assessments.Confirm';");
            // NOTE: Manually add GrantApplicationManagement.Assessments.Confirm for admin, system_admin, team_lead,
        }
    }
}

