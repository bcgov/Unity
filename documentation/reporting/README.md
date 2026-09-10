# Unity Reporting Documentation

Unity Portal exposes grant data to BI tools (currently Metabase) as **PostgreSQL views in the `Reporting` schema**. Everything in this folder is about how those views come into existence, what shape they have, and who maintains them.

There are **two independent view-generation paths** in the codebase today:

| | **Explicit** — Reporting Configuration | **Auto / Dynamic** — legacy |
| --- | --- | --- |
| Status | **Current, go-forward** | **Deprecated — scheduled for removal** |
| Driven by | `Reporting.ReportColumnsMaps` table, filled in by an administrator | `ReportKeys` / `ReportColumns` / `ReportViewName` columns written automatically on publish |
| Triggered by | An admin clicking **Generate View** on the Reporting Configuration tab | Publishing a worksheet/scoresheet, or syncing a CHEFS form version |
| View shape | Typed columns (`NUMERIC`, `TIMESTAMP`, `BOOLEAN`, `DECIMAL(18,2)`, `TEXT`) | Every column `TEXT` |
| Reads from | Source data directly (`Submission`, `WorksheetInstances.CurrentValue`, `Flex.Answers`) | Pre-flattened `ReportData` JSONB snapshots |
| Column names | Admin-controlled, sanitised, uniqueness-enforced | Auto-derived from the source key, truncated at 63 chars |
| Coverage | 5 providers incl. cross-version consolidated views | One view per form version / worksheet / scoresheet |
| DB procedures | `generate_formversion_view`, `generate_worksheet_view`, `generate_scoresheet_view`, `generate_consolidated_formversion_view`, `generate_consolidated_worksheet_view` | `generate_submissions_view`, `generate_worksheets_view`, `generate_scoresheets_view` |

Both paths write into the same `Reporting` schema and are both picked up by the same tenant reporting role, so a database today can contain views from both. The end goal is to remove the Auto path entirely once the reporting team has moved all Metabase reports onto explicitly configured views.

## Read in this order

1. **[reporting-architecture.md](reporting-architecture.md)** — the layer model (raw tables → views → Metabase models → cards), where each of the two paths sits in it, and the use cases each one serves.
2. **[reporting-configuration.md](reporting-configuration.md)** — the **explicit** path in full: the five providers, field metadata, column-name generation and validation, view generation, change detection, role assignment, and the admin UI.
3. **[reporting-auto-generated-views.md](reporting-auto-generated-views.md)** — the **deprecated** Auto/Dynamic path in full: what generates it, what it persists, its known rough edges, and the **phased deprecation plan** (Phase 1 code removal, Phase 2 data/DB removal).

### SQL function specifications (explicit path)

Reference-level specs for the PL/pgSQL functions that build the explicit views' `SELECT` clauses:

- **[get_formversion_data_specification.md](get_formversion_data_specification.md)** — CHEFS submission JSON → columns (`formversion`)
- **[get_consolidated_formversion_data_specification.md](get_consolidated_formversion_data_specification.md)** — same, merged across all versions of a form (`formversion_consolidated`)
- **[get_worksheet_data_specification.md](get_worksheet_data_specification.md)** — Flex worksheet instance JSON → columns (`worksheet`)
- **[get_consolidated_worksheet_data_specification.md](get_consolidated_worksheet_data_specification.md)** — same, merged across all versions (`worksheet_consolidated`)
- **[get_scoresheet_data_specification.md](get_scoresheet_data_specification.md)** — Flex answers (normalised rows, not JSON) → columns, plus `total_score` (`scoresheet`)

There is no equivalent spec for the Auto path — its three procedures are near-identical 58-line copies of one another and are reproduced in full in [reporting-auto-generated-views.md](reporting-auto-generated-views.md).

## Source locations

```
applications/Unity.GrantManager/
├── modules/Unity.Reporting/                          the explicit path (module)
│   └── src/
│       ├── Unity.Reporting.Domain.Shared/            Providers, ViewStatus, RoleStatus, settings
│       ├── Unity.Reporting.Application.Contracts/    DTOs, IReportMappingService, permissions
│       ├── Unity.Reporting.Application/              ReportMappingService, ReportMappingUtils,
│       │                                             5 IFieldsProvider impls, background jobs,
│       │                                             ReportColumnsMap entity + repository + DbContext
│       └── Unity.Reporting.Web/                      ReportingConfiguration view component + controller
├── modules/Unity.Flex/src/Unity.Flex.Application/Reporting/    the Auto path for worksheets/scoresheets
├── src/Unity.GrantManager.Application/Reporting/               the Auto path for CHEFS submissions
└── src/Unity.GrantManager.EntityFrameworkCore/Scripts/         all SQL, deployed as embedded resources
```

All `Reporting` schema objects — both paths — are created by the tenant migration `20260721203242_Initial`, which runs the `Scripts/*.sql` files as embedded resources. See `Scripts/README.md` for the embedded-resource contract.

## Handover pages

One-page visual summaries live in `documentation/handover/`:

- `reporting-configuration-handover.html` — the explicit path
- `reporting-auto-views-handover.html` — the Auto path and its deprecation plan
