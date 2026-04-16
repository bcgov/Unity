using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class RenameAIPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.Analysis.ViewApplicationAnalysis' WHERE \"Name\" = 'AI.ApplicationAnalysis';");
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.Analysis.ViewAttachmentSummary' WHERE \"Name\" = 'AI.AttachmentSummary';");
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.Analysis.ViewScoringResult' WHERE \"Name\" = 'AI.ScoringAssistant';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.ApplicationAnalysis' WHERE \"Name\" = 'AI.Analysis.ViewApplicationAnalysis';");
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.AttachmentSummary' WHERE \"Name\" = 'AI.Analysis.ViewAttachmentSummary';");
            migrationBuilder.Sql($"UPDATE public.\"PermissionGrants\" SET \"Name\" = 'AI.ScoringAssistant' WHERE \"Name\" = 'AI.Analysis.ViewScoringResult';");
        }
    }
}
