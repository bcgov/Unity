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
            RenamePermissionGrant(migrationBuilder, "AI.ApplicationAnalysis", "AI.Analysis.ViewApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.AttachmentSummary", "AI.Analysis.ViewAttachmentSummary");
            RenamePermissionGrant(migrationBuilder, "AI.ScoringAssistant", "AI.Analysis.ViewScoringResult");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewApplicationAnalysis", "AI.ApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewAttachmentSummary", "AI.AttachmentSummary");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewScoringResult", "AI.ScoringAssistant");
        }

        private static void RenamePermissionGrant(MigrationBuilder migrationBuilder, string fromName, string toName)
        {
            migrationBuilder.Sql($@"
DELETE FROM public.""PermissionGrants"" target
USING public.""PermissionGrants"" source
WHERE target.""Name"" = '{toName}'
  AND source.""Name"" = '{fromName}'
  AND target.""TenantId"" IS NOT DISTINCT FROM source.""TenantId""
  AND target.""ProviderName"" IS NOT DISTINCT FROM source.""ProviderName""
  AND target.""ProviderKey"" IS NOT DISTINCT FROM source.""ProviderKey"";

UPDATE public.""PermissionGrants""
SET ""Name"" = '{toName}'
WHERE ""Name"" = '{fromName}';
");
        }
    }
}
