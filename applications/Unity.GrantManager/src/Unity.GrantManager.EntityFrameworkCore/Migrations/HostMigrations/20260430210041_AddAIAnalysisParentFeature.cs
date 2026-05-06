using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AddAIAnalysisParentFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM public.""FeatureValues""
                WHERE ""Name"" = 'Unity.AI.Analysis';
            ");
        }
    }
}
