using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Renumber_UnityApplicationId_And_Seed_Counters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A: Renumber existing sequential UnityApplicationIds to fill historical gaps.
            //
            // Partitioned by (TenantId, Prefix) — tenants sharing a schema get separate,
            // independent sequences.  Prefix comes from the ApplicationForm definition
            // (not inferred from the ID string) to avoid regex metacharacter issues.
            // Only SuffixType = 1 (SequentialNumber) forms are touched.
            // Stable ordering: original suffix number → CreationTime → Id.
            // Malformed IDs (non-numeric suffix) are left untouched.
            migrationBuilder.Sql(@"
                WITH base AS (
                    SELECT
                        a.""Id"",
                        COALESCE(a.""TenantId"", '00000000-0000-0000-0000-000000000000'::UUID) AS tenant_id,
                        af.""Prefix"",
                        a.""CreationTime"",
                        SUBSTRING(a.""UnityApplicationId"" FROM CHAR_LENGTH(af.""Prefix"") + 1) AS suffix
                    FROM ""Applications"" a
                    JOIN ""ApplicationForms"" af ON a.""ApplicationFormId"" = af.""Id""
                    WHERE af.""SuffixType"" = 1
                      AND af.""Prefix"" IS NOT NULL
                      AND af.""Prefix"" <> ''
                      AND a.""UnityApplicationId"" IS NOT NULL
                      AND LEFT(a.""UnityApplicationId"", CHAR_LENGTH(af.""Prefix"")) = af.""Prefix""
                ),
                valid AS (
                    SELECT
                        ""Id"",
                        tenant_id,
                        ""Prefix"",
                        ""CreationTime"",
                        suffix::BIGINT AS old_seq
                    FROM base
                    WHERE suffix ~ '^[0-9]+$'
                ),
                ranked AS (
                    SELECT
                        ""Id"",
                        ""Prefix"",
                        ROW_NUMBER() OVER (
                            PARTITION BY tenant_id, ""Prefix""
                            ORDER BY old_seq, ""CreationTime"", ""Id""
                        ) AS new_seq
                    FROM valid
                )
                UPDATE ""Applications"" a
                SET ""UnityApplicationId"" =
                    r.""Prefix"" || LPAD(r.new_seq::TEXT, GREATEST(5, LENGTH(r.new_seq::TEXT)), '0')
                FROM ranked r
                WHERE a.""Id"" = r.""Id"";
            ");
            

            // B: Seed unity_sequence_counters from the post-renumber maximum.
            //
            // LEFT JOIN from ApplicationForms so that every (tenant, prefix) combination that
            // is configured for sequential numbering gets a counter row — even forms that have
            // zero applications yet.  Those are seeded with current_value = 0, so the first
            // real upsert increments to 1 without a gap.
            migrationBuilder.Sql(@"
                WITH parsed AS (
                    SELECT
                        COALESCE(a.""TenantId"", af.""TenantId"", '00000000-0000-0000-0000-000000000000'::UUID) AS tenant_id,
                        af.""Prefix"" AS prefix,
                        CASE
                            WHEN a.""UnityApplicationId"" IS NOT NULL
                             AND LEFT(a.""UnityApplicationId"", CHAR_LENGTH(af.""Prefix"")) = af.""Prefix""
                             AND SUBSTRING(a.""UnityApplicationId"" FROM CHAR_LENGTH(af.""Prefix"") + 1) ~ '^[0-9]+$'
                            THEN CAST(SUBSTRING(a.""UnityApplicationId"" FROM CHAR_LENGTH(af.""Prefix"") + 1) AS BIGINT)
                            ELSE NULL
                        END AS seq
                    FROM ""ApplicationForms"" af
                    LEFT JOIN ""Applications"" a ON a.""ApplicationFormId"" = af.""Id""
                    WHERE af.""SuffixType"" = 1
                      AND af.""Prefix"" IS NOT NULL
                      AND af.""Prefix"" <> ''
                )
                INSERT INTO ""unity_sequence_counters"" (""tenant_id"", ""prefix"", ""current_value"")
                SELECT tenant_id, prefix, COALESCE(MAX(seq), 0)
                FROM parsed
                GROUP BY tenant_id, prefix
                ON CONFLICT (""tenant_id"", ""prefix"") DO UPDATE
                SET ""current_value"" = GREATEST(
                    ""unity_sequence_counters"".""current_value"",
                    EXCLUDED.""current_value""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Counter rows are removed; un-renumbering IDs is not supported.
            // Full rollback requires restoring from a pre-migration backup.
            migrationBuilder.Sql(@"DELETE FROM ""unity_sequence_counters"";");
        }
    }
}
