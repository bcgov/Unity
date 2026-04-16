using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Backfill_UnityApplicationId_ABPP2025FallClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A: Assign sequential UnityApplicationIds to applications with NULL values
            //    belonging to the "ABPP 2025 - Fall Claim for Payment" form.
            //
            // Reads the current counter from unity_sequence_counters and adds ROW_NUMBER()
            // per (tenant, form) partition ordered by SubmissionDate → CreationTime → Id,
            // matching the stable ordering used by SequenceRepository.
            migrationBuilder.Sql(@"
                WITH target_forms AS (
                    SELECT
                        af.""Id""                                                               AS form_id,
                        af.""Prefix"",
                        COALESCE(af.""TenantId"", '00000000-0000-0000-0000-000000000000'::UUID) AS tenant_id
                    FROM ""ApplicationForms"" af
                    WHERE af.""Category""   = 'ABPP 2025 - Fall Claim for Payment'
                      AND af.""SuffixType"" = 1
                      AND af.""Prefix""     IS NOT NULL
                      AND af.""Prefix""     <> ''
                ),
                counters AS (
                    SELECT sc.""current_value"", tf.tenant_id, tf.""Prefix""
                    FROM ""unity_sequence_counters"" sc
                    JOIN target_forms tf ON sc.""tenant_id"" = tf.tenant_id AND sc.""prefix"" = tf.""Prefix""
                ),
                null_apps AS (
                    SELECT
                        a.""Id"",
                        tf.form_id,
                        tf.""Prefix"",
                        tf.tenant_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY tf.tenant_id, tf.form_id
                            ORDER BY a.""SubmissionDate"" ASC, a.""CreationTime"" ASC, a.""Id"" ASC
                        ) AS rn
                    FROM ""Applications"" a
                    JOIN target_forms tf ON a.""ApplicationFormId"" = tf.form_id
                    WHERE a.""UnityApplicationId"" IS NULL
                ),
                assignments AS (
                    SELECT
                        na.""Id"",
                        na.""Prefix"" || LPAD(
                            (c.""current_value"" + na.rn)::TEXT,
                            GREATEST(5, LENGTH((c.""current_value"" + na.rn)::TEXT)),
                            '0'
                        ) AS new_unity_id
                    FROM null_apps na
                    JOIN counters c ON c.tenant_id = na.tenant_id AND c.""Prefix"" = na.""Prefix""
                )
                UPDATE ""Applications"" a
                SET ""UnityApplicationId"" = asgn.new_unity_id
                FROM assignments asgn
                WHERE a.""Id"" = asgn.""Id"";
            ");

            // B: Sync unity_sequence_counters to the new maximum after the backfill.
            //    Uses GREATEST so the counter never moves backwards.
            migrationBuilder.Sql(@"
                WITH target_forms AS (
                    SELECT
                        af.""Id""                                                               AS form_id,
                        af.""Prefix"",
                        COALESCE(af.""TenantId"", '00000000-0000-0000-0000-000000000000'::UUID) AS tenant_id
                    FROM ""ApplicationForms"" af
                    WHERE af.""Category""   = 'ABPP 2025 - Fall Claim for Payment'
                      AND af.""SuffixType"" = 1
                      AND af.""Prefix""     IS NOT NULL
                      AND af.""Prefix""     <> ''
                ),
                new_max AS (
                    SELECT
                        tf.tenant_id,
                        tf.""Prefix"",
                        MAX(
                            CAST(
                                SUBSTRING(a.""UnityApplicationId"" FROM CHAR_LENGTH(tf.""Prefix"") + 1)
                                AS BIGINT
                            )
                        ) AS max_seq
                    FROM ""Applications"" a
                    JOIN target_forms tf ON a.""ApplicationFormId"" = tf.form_id
                    WHERE a.""UnityApplicationId"" IS NOT NULL
                      AND LEFT(a.""UnityApplicationId"", CHAR_LENGTH(tf.""Prefix"")) = tf.""Prefix""
                      AND SUBSTRING(a.""UnityApplicationId"" FROM CHAR_LENGTH(tf.""Prefix"") + 1) ~ '^[0-9]+$'
                    GROUP BY tf.tenant_id, tf.""Prefix""
                )
                UPDATE ""unity_sequence_counters"" sc
                SET ""current_value"" = GREATEST(sc.""current_value"", nm.max_seq)
                FROM new_max nm
                WHERE sc.""tenant_id"" = nm.tenant_id
                  AND sc.""prefix""    = nm.""Prefix"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Un-assigning backfilled IDs is not supported.
            // Rollback requires restoring from a pre-migration backup.
        }
    }
}
