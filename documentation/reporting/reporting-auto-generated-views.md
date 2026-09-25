# Auto-Generated Reporting Views (Dynamic) — **Deprecated**

> **Status: generation removed; database objects still present.** No application code creates, refreshes, or feeds these views any more. The views, the three procedures that built them, and the columns they read remain in every tenant database until the [Phase 2](#phase-2--remove-the-data-and-database-objects) migration drops them. New reporting views are created only through [Reporting Configuration](reporting-configuration.md).

## Overview

The Auto (also called *Dynamic*) path created one PostgreSQL view per **CHEFS form version**, per **published worksheet**, and per **published scoresheet**, with no administrator involvement:

1. Flattening the source definition into a pipe-delimited list of **report keys** and matching **report columns**, stored on the definition row itself.
2. Flattening each instance's answers into a `ReportData` JSONB snapshot keyed by those report keys.
3. Calling a stored procedure that read the key/column lists off the definition row and emitted a view of `TEXT` columns, each one a lookup into `ReportData` at query time.

Steps 1 and 2 no longer happen, and nothing calls step 3.

---

## What remains in the database

### Views

| Source | Pattern | Example | Explicit replacement provider |
| --- | --- | --- | --- |
| Form version | `Form-{ApplicationFormName}-V{Version}` | `Form-Community Grants-V3` | `formversion`, or `formversion_consolidated` |
| Worksheet | `Worksheet-{Worksheet.Name}` | `Worksheet-project_budget-v2` | `worksheet`, or `worksheet_consolidated` |
| Scoresheet | `Scoresheet-{Scoresheet.Name}` | `Scoresheet-standard_review` | `scoresheet` |

These names are mixed-case and contain hyphens and (for forms) spaces — see [Known rough edges](#known-rough-edges).

**The data behind these views is frozen.** Because each view reads `ReportData` at query time and nothing writes `ReportData` any more:

- Submissions, worksheet instances, and scoresheet instances **created after generation was removed** appear in their view with every generated column empty (`''`), and `TotalScore` as `0`.
- Instances **edited after generation was removed** keep showing their earlier values.
- New form versions, worksheets, and scoresheets get no auto view at all.

Metabase reports built on an auto view therefore keep running but stop reflecting new data. Re-point them at an explicitly configured view.

### Definition columns — `ReportKeys`, `ReportColumns`, `ReportViewName`

All three are `text NOT NULL`, and are now plain persisted properties with no writer.

| Table | Schema |
| --- | --- |
| `ApplicationFormVersion` | `public` |
| `Worksheets` | `Flex` |
| `Scoresheets` | `Flex` |

`ReportKeys` and `ReportColumns` are two parallel `|`-delimited lists. The key is the lookup into `ReportData`; the column is the resulting SQL identifier. New definition rows get empty strings.

### Instance columns — `ReportData`

`jsonb NOT NULL` on `public.ApplicationFormSubmissions`, `Flex.WorksheetInstances`, and `Flex.ScoresheetInstances`. New rows get `{}`. For scoresheet instances it also carried a `TotalScore` entry.

### The stored procedures

`generate_submissions_view`, `generate_worksheets_view`, and `generate_scoresheets_view` still exist in the `Reporting` schema (created by `20260721203242_Initial` from `Scripts/generate_*s_view.sql`), but no code calls them. They are effectively the same 58-line procedure three times over:

```sql
CREATE OR REPLACE PROCEDURE "Reporting".generate_worksheets_view(IN table_a_id uuid)
...
    SELECT "ReportViewName", "ReportColumns", "ReportKeys" INTO view_name, view_columns, view_keys
    FROM "Flex"."Worksheets" WHERE "Id" = table_a_id;
    ...
    select_clause := ... format(
        'COALESCE((SELECT value::TEXT FROM jsonb_each_text("ReportData") WHERE key = %L LIMIT 1), '''') AS %I',
        key_names[i], column_names[i]);

    EXECUTE format('DROP VIEW IF EXISTS %I', view_name);
    EXECUTE format('CREATE VIEW "Reporting".%I AS SELECT ... %s FROM (...) AS subquery', view_name, select_clause, table_a_id);
```

| Procedure | Definition table | Instance table | Passthrough columns |
| --- | --- | --- | --- |
| `generate_submissions_view` | `public.ApplicationFormVersion` | `public.ApplicationFormSubmissions` (filtered on `ApplicationFormVersionId`) | `Id`, `ApplicationId` |
| `generate_worksheets_view` | `Flex.Worksheets` | `Flex.WorksheetInstances` (filtered on `WorksheetId`) | `Id`, `CorrelationId`, `CorrelationProvider` |
| `generate_scoresheets_view` | `Flex.Scoresheets` | `Flex.ScoresheetInstances` (filtered on `ScoresheetId`) | `Id`, `CorrelationId`, `CorrelationProvider`, `TotalScore` |

`TotalScore` is the only non-`TEXT` column any of the three produces: `COALESCE(("ReportData"->>'TotalScore')::integer, 0)`.

### What no longer exists

- The IT-Admin backfill services `FormsReportSyncServiceAppService`, `WorksheetReportingFieldsSyncAppService`, and `ScoresheetReportingFieldsSyncAppService`, which had no UI but were reachable through ABP's auto-generated API. Scripts calling them now receive a 404.
- `ReportColumns` / `ReportKeys` / `ReportViewName` in the AI form-scoresheet contract (`FormScoresheetResponse`, `AIProviderPayloadValidator.ValidateFormScoresheetJson`, and the built-in `FormScoresheet` v2 prompt). `AIPromptDataSeeder` overwrites built-in prompt text on every host seed.
- The report fields on `ApplicationFormVersionDto`, `WorksheetDto`, `CreateWorksheetDto`, and `CreateWorksheetInstanceDto`.

---

## Known rough edges

These are the reasons the path is being retired.

1. **Everything is `TEXT`.** Numbers, dates, currency, and booleans all arrive as strings, so every Metabase model on top of an auto view has to cast. The explicit path emits real `NUMERIC` / `DECIMAL(18,2)` / `TIMESTAMP` / `BOOLEAN` columns via the `Reporting.safe_to_*` helpers.
2. **View names are not valid bare identifiers.** `Form-…`, `Worksheet-…`, `Scoresheet-…` all contain a hyphen. `ReportColumnsMapRepository.IsValidPostgreSqlIdentifier` accepts only `^[a-zA-Z_][a-zA-Z0-9_]*$`, and `AssignRoleToAllViewsAsync` validates *every* view name it reads back from `pg_views` in the `Reporting` schema before granting. An auto view in the schema therefore makes the "assign role to all views" operation throw `ArgumentException` part-way through, leaving the grant loop incomplete. This lasts until Phase 2 drops the views.
3. **The data is frozen** — see [Views](#views).
4. **`ReportData` is a second copy of the answers.** It duplicates data that already exists in `Submission`, `WorksheetInstances.CurrentValue`, and `Flex.Answers`.
5. **No cross-version story.** One view per form version means a report that spans versions has to be assembled by hand in Metabase. `formversion_consolidated` / `worksheet_consolidated` in the explicit path exist precisely to solve this.

---

## Removal

### Prerequisite: the explicit path does not depend on any of this

Verified against the SQL: none of `get_formversion_data`, `get_consolidated_formversion_data`, `get_worksheet_data`, `get_consolidated_worksheet_data`, or `get_scoresheet_data` reads `ReportData`, `ReportKeys`, `ReportColumns`, or `ReportViewName`. They read source data directly:

| Explicit provider | Reads from |
| --- | --- |
| `formversion`, `formversion_consolidated` | `public.ApplicationFormSubmissions."Submission"` |
| `worksheet`, `worksheet_consolidated` | `Flex.WorksheetInstances."CurrentValue"` joined to `Flex.Worksheets` |
| `scoresheet` | `Flex.ScoresheetInstances` → `Assessments` → `Applications`, values from `Flex.Answers` / `Flex.Questions`, total from `Reporting.calculate_scoresheet_total_score` |

So dropping the Auto path's columns cannot break an explicitly configured view.

### Phase 1 — stop generating (done)

All generation code and its call sites are gone: nothing runs on worksheet/scoresheet publish, CHEFS form-version sync, intake, instance save, or data-grid row creation. No tenant migration is involved, and the change is reversible by reverting it.

**Verification:** on a tenant with the `Unity.Reporting` feature enabled, publish a worksheet and a scoresheet and re-sync a CHEFS form version. No new rows appear in `pg_views` under `Reporting`, `ReportViewName` stays empty on the new definitions, and a new submission's `ReportData` is `{}`.

### Phase 2 — remove the data and database objects

**Precondition (hard gate):** the reporting team confirms that no Metabase model, question, or dashboard — and no other external consumer — references any `Form-*`, `Worksheet-*`, or `Scoresheet-*` view, and that nothing reads `ReportData->>'TotalScore'`. Inventory the views per tenant — every auto view selects `ReportData`, so they can be found through their column dependency:

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

**One tenant migration, in order:**

1. **Drop every view found by the query above**, without `CASCADE`, so a view someone built on top of an auto view makes the migration fail for that tenant instead of being dropped silently.
2. **Drop the three procedures**, delete `Scripts/generate_{submissions,worksheets,scoresheets}_view.sql` with their entries in `Unity.GrantManager.EntityFrameworkCore.csproj`, and remove their `RunEmbeddedScript` lines from `20260721203242_Initial.cs`.
3. **Drop the columns** — `ReportKeys`, `ReportColumns`, `ReportViewName` from `ApplicationFormVersion`, `Flex.Worksheets`, `Flex.Scoresheets`, and `ReportData` from `ApplicationFormSubmissions`, `Flex.WorksheetInstances`, `Flex.ScoresheetInstances` — and remove the matching entity properties.

Generate it with `--context GrantTenantDbContext --output-dir Migrations/TenantMigrations`, and take a database backup before running it in any environment — `DROP COLUMN` is irreversible.

**Verification:** `SELECT viewname FROM pg_views WHERE schemaname = 'Reporting'` returns exactly the `ViewName` set from `Reporting."ReportColumnsMaps"`, the three procedures are gone from `pg_proc`, and "assign role to all views" completes for the tenant (see rough edge 2).

### Sequencing summary

| | Phase 1 | Phase 2 |
| --- | --- | --- |
| State | Done | Pending |
| Blocked on | — | Reporting team's Metabase migration complete |
| Removes | Generation code and its call sites | Views, procedures, and all six tables' report columns |
| Existing Metabase reports on auto views | Keep running; data frozen | Break unless already re-pointed |
| Reversible | Yes (revert the commit) | No (data loss) |
| Tenant migration needed | No | Yes |
