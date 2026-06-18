using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    [DbContext(typeof(GrantManagerDbContext))]
    [Migration("20260618210000_RefreshBuiltInAIPromptVersions")]
    public partial class RefreshBuiltInAIPromptVersions : Migration
    {
        private static readonly Guid AnalysisPromptId = new("4a100001-1000-4000-a000-000000000001");
        private static readonly Guid AttachmentPromptId = new("4a100001-1000-4000-a000-000000000002");
        private static readonly Guid ScoringPromptId = new("4a100001-1000-4000-a000-000000000003");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "AI"."AIPrompts"
                SET "Name" = 'ApplicationAnalysis',
                    "Description" = 'Grant application analysis and review',
                    "Type" = 1,
                    "IsActive" = TRUE
                WHERE "Id" = '4a100001-1000-4000-a000-000000000001';

                UPDATE "AI"."AIPrompts"
                SET "Name" = 'AttachmentSummary',
                    "Description" = 'Attachment summarization for grant review',
                    "Type" = 1,
                    "IsActive" = TRUE
                WHERE "Id" = '4a100001-1000-4000-a000-000000000002';

                UPDATE "AI"."AIPrompts"
                SET "Name" = 'ApplicationScoring',
                    "Description" = 'Scoresheet section answering assistant',
                    "Type" = 1,
                    "IsActive" = TRUE
                WHERE "Id" = '4a100001-1000-4000-a000-000000000003';
                """);

            UpdatePromptVersion(
                migrationBuilder,
                AnalysisPromptId,
                0,
                ReadPromptFile("v0", "application-analysis.system.txt"),
                ReadPromptFile("v0", "application-analysis.user.txt"),
                "v0 - initial single-file analysis prompt",
                null);

            UpdatePromptVersion(
                migrationBuilder,
                AnalysisPromptId,
                1,
                ReadPromptFile("v1", "application-analysis.system.txt"),
                ReadPromptFile("v1", "application-analysis.user.txt"),
                "v1 - modular prompt with separate rubric, score, output, and rules sections",
                BuildSectionsJson(new Dictionary<string, string>
                {
                    ["RUBRIC"] = ReadPromptFile("v1", "application-analysis.rubric.txt"),
                    ["SCORE"] = ReadPromptFile("v1", "application-analysis.score.txt"),
                    ["OUTPUT"] = ReadPromptFile("v1", "application-analysis.output.txt"),
                    ["RULES"] = ReadPromptFile("v1", "application-analysis.rules.txt"),
                    ["COMMON_RULES"] = ReadPromptFile("v1", "common.rules.txt")
                }));

            UpdatePromptVersion(
                migrationBuilder,
                AttachmentPromptId,
                0,
                ReadPromptFile("v0", "attachment-summary.system.txt"),
                ReadPromptFile("v0", "attachment-summary.user.txt"),
                "v0 - initial single-file attachment prompt",
                null);

            UpdatePromptVersion(
                migrationBuilder,
                AttachmentPromptId,
                1,
                ReadPromptFile("v1", "attachment-summary.system.txt"),
                ReadPromptFile("v1", "attachment-summary.user.txt"),
                "v1 - modular prompt with separate output and rules sections",
                BuildSectionsJson(new Dictionary<string, string>
                {
                    ["OUTPUT"] = ReadPromptFile("v1", "attachment-summary.output.txt"),
                    ["RULES"] = ReadPromptFile("v1", "attachment-summary.rules.txt"),
                    ["COMMON_RULES"] = ReadPromptFile("v1", "common.rules.txt")
                }));

            UpdatePromptVersion(
                migrationBuilder,
                ScoringPromptId,
                0,
                ReadPromptFile("v0", "application-scoring.system.txt"),
                ReadPromptFile("v0", "application-scoring.user.txt"),
                "v0 - initial single-file scoresheet prompt",
                null);

            UpdatePromptVersion(
                migrationBuilder,
                ScoringPromptId,
                1,
                ReadPromptFile("v1", "application-scoring.system.txt"),
                ReadPromptFile("v1", "application-scoring.user.txt"),
                "v1 - modular prompt with separate output and rules sections",
                BuildSectionsJson(new Dictionary<string, string>
                {
                    ["OUTPUT"] = ReadPromptFile("v1", "application-scoring.output.txt"),
                    ["RULES"] = ReadPromptFile("v1", "application-scoring.rules.txt"),
                    ["COMMON_RULES"] = ReadPromptFile("v1", "common.rules.txt")
                }));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: prompt version content is managed by seeded defaults.
        }

        private static void UpdatePromptVersion(
            MigrationBuilder migrationBuilder,
            Guid promptId,
            int versionNumber,
            string systemPrompt,
            string userPromptTemplate,
            string developerNotes,
            string metadataJson)
        {
            migrationBuilder.Sql($"""
                UPDATE "AI"."AIPromptVersions"
                SET "SystemPrompt" = '{EscapeSqlLiteral(systemPrompt)}',
                    "UserPromptTemplate" = '{EscapeSqlLiteral(userPromptTemplate)}',
                    "DeveloperNotes" = '{EscapeSqlLiteral(developerNotes)}',
                    "IsPublished" = TRUE,
                    "IsDeprecated" = FALSE,
                    "MetadataJson" = {FormatNullableSqlString(metadataJson)}
                WHERE "PromptId" = '{promptId}'
                  AND "VersionNumber" = {versionNumber};
                """);
        }

        private static string ReadPromptFile(string version, string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "AI", "Prompts", "Versions", version, fileName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Missing prompt asset required by migration: {path}");
            }

            return File.ReadAllText(path);
        }

        private static string BuildSectionsJson(Dictionary<string, string> sections)
        {
            return JsonSerializer.Serialize(new { sections });
        }

        private static string EscapeSqlLiteral(string value)
        {
            return value.Replace("'", "''", StringComparison.Ordinal);
        }

        private static string FormatNullableSqlString(string value)
        {
            if (value == null)
            {
                return "NULL";
            }

            return $"'{EscapeSqlLiteral(value)}'";
        }
    }
}
