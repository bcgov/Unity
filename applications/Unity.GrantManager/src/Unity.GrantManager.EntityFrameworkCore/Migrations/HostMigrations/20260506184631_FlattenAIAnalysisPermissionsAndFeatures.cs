using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class FlattenAIAnalysisPermissionsAndFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM public.""PermissionGrants""
                WHERE ""Name"" = 'AI.Analysis';

                DELETE FROM public.""FeatureValues""
                WHERE ""Name"" = 'Unity.AI.Analysis';
            ");

            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewApplicationAnalysis",    "AI.ViewApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewAttachmentSummary",       "AI.ViewAttachmentSummary");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.ViewScoringResult",           "AI.ViewScoringResult");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.GenerateApplicationAnalysis", "AI.GenerateApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.GenerateAttachmentSummaries", "AI.GenerateAttachmentSummaries");
            RenamePermissionGrant(migrationBuilder, "AI.Analysis.GenerateScoring",             "AI.GenerateScoring");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RenamePermissionGrant(migrationBuilder, "AI.ViewApplicationAnalysis",    "AI.Analysis.ViewApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.ViewAttachmentSummary",       "AI.Analysis.ViewAttachmentSummary");
            RenamePermissionGrant(migrationBuilder, "AI.ViewScoringResult",           "AI.Analysis.ViewScoringResult");
            RenamePermissionGrant(migrationBuilder, "AI.GenerateApplicationAnalysis", "AI.Analysis.GenerateApplicationAnalysis");
            RenamePermissionGrant(migrationBuilder, "AI.GenerateAttachmentSummaries", "AI.Analysis.GenerateAttachmentSummaries");
            RenamePermissionGrant(migrationBuilder, "AI.GenerateScoring",             "AI.Analysis.GenerateScoring");

            migrationBuilder.Sql(@"
                INSERT INTO public.""FeatureValues"" (""Id"", ""Name"", ""Value"", ""ProviderName"", ""ProviderKey"")
                SELECT gen_random_uuid(), 'Unity.AI.Analysis', 'True', ""ProviderName"", ""ProviderKey""
                FROM public.""FeatureValues""
                WHERE ""Name"" IN (
                    'Unity.AI.ApplicationAnalysis',
                    'Unity.AI.AttachmentSummaries',
                    'Unity.AI.Scoring'
                )
                AND LOWER(""Value"") = 'true'
                AND NOT EXISTS (
                    SELECT 1 FROM public.""FeatureValues"" e2
                    WHERE e2.""Name"" = 'Unity.AI.Analysis'
                      AND e2.""ProviderName"" IS NOT DISTINCT FROM ""FeatureValues"".""ProviderName""
                      AND e2.""ProviderKey"" IS NOT DISTINCT FROM ""FeatureValues"".""ProviderKey""
                )
                GROUP BY ""ProviderName"", ""ProviderKey"";
            ");
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
