using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33799_FixCheckboxGroupEmptyValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-off backfill for AB#33799: root-level checkbox-group fields (e.g. HowDidYouHearAboutUs,
            // TrainingEducation, BeneficiaryGroups) that were saved with an empty string / non-JSON-array
            // value instead of null. That shape breaks the reporting views' ::jsonb cast with
            // "invalid input syntax for type json". Reuses the checkbox-group field-key detection from
            // "Reporting"."ReportColumnsMaps" to find every worksheet/field affected, not just the ones
            // already found manually on UAT. Idempotent: once fixed, the value is JSON null and no
            // longer matches the WHERE clause, so re-running this migration is a no-op.
            migrationBuilder.Sql(@"
                WITH checkboxgroup_fields AS (
                    SELECT DISTINCT
                        split_part(
                            COALESCE(
                                CASE
                                    WHEN row_data->>'DataPath' ~ '^\('
                                    THEN regexp_replace(row_data->>'DataPath', '^\([^)]+\)', '')
                                    ELSE row_data->>'DataPath'
                                END,
                                row_data->>'PropertyName'
                            ), '->', 1
                        ) AS field_key
                    FROM ""Reporting"".""ReportColumnsMaps"" rcm,
                         jsonb_array_elements(rcm.""Mapping""->'Rows') AS row_data
                    WHERE row_data->>'TypePath' ILIKE '%checkboxgroup%'
                      AND row_data->>'TypePath' NOT ILIKE '%datagrid%'
                )
                UPDATE ""Flex"".""WorksheetInstances"" wi
                SET ""CurrentValue"" = jsonb_set(
                    wi.""CurrentValue"",
                    '{values}',
                    (
                        SELECT jsonb_agg(
                            CASE
                                WHEN elem->>'key' IN (SELECT field_key FROM checkboxgroup_fields)
                                     AND elem->>'value' IS NOT NULL
                                     AND jsonb_typeof(""Reporting"".safe_to_jsonb(elem->>'value')) IS DISTINCT FROM 'array'
                                THEN jsonb_set(elem, '{value}', 'null'::jsonb)
                                ELSE elem
                            END
                        )
                        FROM jsonb_array_elements(wi.""CurrentValue""->'values') AS elem
                    )
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(wi.""CurrentValue""->'values') AS elem
                    WHERE elem->>'key' IN (SELECT field_key FROM checkboxgroup_fields)
                      AND elem->>'value' IS NOT NULL
                      AND jsonb_typeof(""Reporting"".safe_to_jsonb(elem->>'value')) IS DISTINCT FROM 'array'
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: the original (invalid) empty-string values are not preserved, and re-introducing
            // them would just reproduce the bug this migration fixes.
        }
    }
}
