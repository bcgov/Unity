# Auto-Generated Reporting Views — Removed

> Unity Portal creates reporting views only through [Reporting Configuration](reporting-configuration.md). The older auto-generated views, and the data that fed them, are dropped from every tenant database by the tenant migration `20260911160335_AB34344_RemoveAutoReportingViews`. **That migration is destructive and has a pre-deployment gate** — see [Before deploying](#before-deploying).

## What the auto views were

One PostgreSQL view per CHEFS form version, per published Flex worksheet, and per published Flex scoresheet, created on publish with no administrator involvement:

| Source | View name | Created by | Explicit replacement provider |
| --- | --- | --- | --- |
| CHEFS form version | `Reporting."Form-{ApplicationFormName}-V{Version}"` | `Reporting.generate_submissions_view(uuid)` | `formversion`, or `formversion_consolidated` for a cross-version view |
| Flex worksheet | `Reporting."Worksheet-{Worksheet.Name}"` | `Reporting.generate_worksheets_view(uuid)` | `worksheet`, or `worksheet_consolidated` |
| Flex scoresheet | `Reporting."Scoresheet-{Scoresheet.Name}"` | `Reporting.generate_scoresheets_view(uuid)` | `scoresheet` |

Each procedure read two parallel pipe-delimited lists — `ReportKeys` and `ReportColumns` — plus `ReportViewName` off the definition row, and emitted one `TEXT` column per key as a lookup into a `ReportData` JSONB snapshot on each instance row. The scoresheet view also exposed an `integer` `TotalScore` read from `ReportData->>'TotalScore'`.

When re-pointing a Metabase model from an auto view to an explicit one, expect three differences: explicit column names are chosen by the administrator rather than derived from the source key; columns are typed (`NUMERIC`, `DECIMAL(18,2)`, `TIMESTAMP`, `BOOLEAN`) rather than all `TEXT`, so casts in native SQL questions can be removed; and the scoresheet total is the `total_score` column computed by `Reporting.calculate_scoresheet_total_score`.

## What the migration removes

`AB34344_RemoveAutoReportingViews` runs against `GrantTenantDbContext` and, in one transaction per tenant:

1. **Drops every view that selects `ReportData`** from `public."ApplicationFormSubmissions"`, `"Flex"."WorksheetInstances"`, or `"Flex"."ScoresheetInstances"`. Views are found through their column dependency in `pg_depend`, not by name, so a view left behind under an old name (for example after a worksheet rename) is still caught. The drop does **not** use `CASCADE` — see [If the migration fails](#if-the-migration-fails).
2. **Drops the three procedures** `Reporting.generate_submissions_view`, `generate_worksheets_view`, and `generate_scoresheets_view`.
3. **Drops the columns:**

   | Table | Columns |
   | --- | --- |
   | `public."ApplicationFormVersion"` | `ReportKeys`, `ReportColumns`, `ReportViewName` |
   | `"Flex"."Worksheets"` | `ReportKeys`, `ReportColumns`, `ReportViewName` |
   | `"Flex"."Scoresheets"` | `ReportKeys`, `ReportColumns`, `ReportViewName` |
   | `public."ApplicationFormSubmissions"` | `ReportData` |
   | `"Flex"."WorksheetInstances"` | `ReportData` |
   | `"Flex"."ScoresheetInstances"` | `ReportData` |

`20260721203242_Initial` does not create the three procedures, so a tenant provisioned from scratch never has them; it still creates the columns, which this migration then drops.

`Down()` re-adds the columns with empty defaults only. The dropped data, views, and procedures cannot be restored from the migration.

### Explicitly configured views are unaffected

None of `get_formversion_data`, `get_consolidated_formversion_data`, `get_worksheet_data`, `get_consolidated_worksheet_data`, or `get_scoresheet_data` reads `ReportData`, `ReportKeys`, `ReportColumns`, or `ReportViewName`. They read source data directly:

| Explicit provider | Reads from |
| --- | --- |
| `formversion`, `formversion_consolidated` | `public.ApplicationFormSubmissions."Submission"` |
| `worksheet`, `worksheet_consolidated` | `Flex.WorksheetInstances."CurrentValue"` joined to `Flex.Worksheets` |
| `scoresheet` | `Flex.ScoresheetInstances` → `Assessments` → `Applications`, values from `Flex.Answers` / `Flex.Questions`, total from `Reporting.calculate_scoresheet_total_score` |

## Before deploying

**Hard gate:** the reporting team confirms that no Metabase model, question, or dashboard — and no other external consumer — references any auto view, and that nothing reads `ReportData->>'TotalScore'`. After the migration, any such reference fails with `relation does not exist`.

Inventory the views the migration will drop, per tenant database. This is the same dependency lookup the migration uses:

```sql
SELECT DISTINCT r.ev_class::regclass AS auto_view
FROM pg_depend d
JOIN pg_rewrite r ON r.oid = d.objid
JOIN pg_class v ON v.oid = r.ev_class
JOIN pg_attribute a ON a.attrelid = d.refobjid AND a.attnum = d.refobjsubid
WHERE d.classid = 'pg_rewrite'::regclass
  AND d.refclassid = 'pg_class'::regclass
  AND d.refobjid IN ('public."ApplicationFormSubmissions"'::regclass,
                     '"Flex"."WorksheetInstances"'::regclass,
                     '"Flex"."ScoresheetInstances"'::regclass)
  AND a.attname = 'ReportData'
  AND v.relkind = 'v'
ORDER BY 1;
```

Cross-check against the explicitly configured views, which are exactly the `ViewName` values in `Reporting."ReportColumnsMaps"`.

Take a database backup before running the migration in any environment.

### If the migration fails

If another view — for example one created by hand for a Metabase native query — is built on top of an auto view, the migration stops at step 1 with:

```
ERROR:  cannot drop view "Reporting"."Worksheet-…" because other objects depend on it
DETAIL:  view … depends on view "Reporting"."Worksheet-…"
```

Nothing is dropped for that tenant. Re-point or drop the dependent view, then re-run the migrator.

## Verification

After the migration, on each tenant database:

- `SELECT viewname FROM pg_views WHERE schemaname = 'Reporting'` returns only the `ViewName` set from `Reporting."ReportColumnsMaps"`.
- `SELECT proname FROM pg_proc WHERE proname IN ('generate_submissions_view', 'generate_worksheets_view', 'generate_scoresheets_view')` returns no rows.
- `SELECT table_name, column_name FROM information_schema.columns WHERE column_name IN ('ReportData', 'ReportKeys', 'ReportColumns', 'ReportViewName')` returns no rows.
- "Assign role to all views" completes for the tenant. `ReportColumnsMapRepository.AssignRoleToAllViewsAsync` rejects any view name that is not a bare identifier, and the hyphenated auto view names were the ones that made it throw part-way through.

## Removed application surface

The removal also deleted the IT-Admin backfill services that had no UI but were reachable through ABP's auto-generated API — `FormsReportSyncServiceAppService`, `WorksheetReportingFieldsSyncAppService`, and `ScoresheetReportingFieldsSyncAppService` — so any script calling them now receives a 404.

The AI form-scoresheet contract (`FormScoresheetResponse`, `AIProviderPayloadValidator.ValidateFormScoresheetJson`, and the built-in `FormScoresheet` v2 prompt) no longer has `ReportColumns`, `ReportKeys`, or `ReportViewName`. `AIPromptDataSeeder` overwrites the built-in prompt text on every host seed, so no separate data update is needed.
