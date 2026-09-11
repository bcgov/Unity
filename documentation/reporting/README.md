# Unity Reporting Documentation

Unity Portal exposes grant data to BI tools (currently Metabase) as **PostgreSQL views in the `Reporting` schema**. Everything in this folder is about how those views come into existence, what shape they have, and who maintains them.

Views are created by **Reporting Configuration**: an administrator maps fields for a source in the `Reporting.ReportColumnsMaps` table and clicks **Generate View**. The resulting views have typed columns (`NUMERIC`, `TIMESTAMP`, `BOOLEAN`, `DECIMAL(18,2)`, `TEXT`), admin-controlled sanitised column names, and read source data directly (`Submission`, `WorksheetInstances.CurrentValue`, `Flex.Answers`). Five providers are supported, including cross-version consolidated views.

An older mechanism that auto-generated one all-`TEXT` view per form version, worksheet, and scoresheet (`Form-*`, `Worksheet-*`, `Scoresheet-*`) has been removed; the tenant migration that drops its views and columns carries a pre-deployment gate — see [reporting-auto-generated-views.md](reporting-auto-generated-views.md).

## Read in this order

1. **[reporting-architecture.md](reporting-architecture.md)** — the layer model (raw tables → views → Metabase models → cards) and the use cases each view provider serves.
2. **[reporting-configuration.md](reporting-configuration.md)** — Reporting Configuration in full: the five providers, field metadata, column-name generation and validation, view generation, change detection, role assignment, and the admin UI.
3. **[reporting-auto-generated-views.md](reporting-auto-generated-views.md)** — the removed auto-generated views: what the `AB34344_RemoveAutoReportingViews` tenant migration drops, the Metabase check that must pass before it is deployed, and how to verify it.

### SQL function specifications

Reference-level specs for the PL/pgSQL functions that build each view's `SELECT` clause:

- **[get_formversion_data_specification.md](get_formversion_data_specification.md)** — CHEFS submission JSON → columns (`formversion`)
- **[get_consolidated_formversion_data_specification.md](get_consolidated_formversion_data_specification.md)** — same, merged across all versions of a form (`formversion_consolidated`)
- **[get_worksheet_data_specification.md](get_worksheet_data_specification.md)** — Flex worksheet instance JSON → columns (`worksheet`)
- **[get_consolidated_worksheet_data_specification.md](get_consolidated_worksheet_data_specification.md)** — same, merged across all versions (`worksheet_consolidated`)
- **[get_scoresheet_data_specification.md](get_scoresheet_data_specification.md)** — Flex answers (normalised rows, not JSON) → columns, plus `total_score` (`scoresheet`)

## Source locations

```
applications/Unity.GrantManager/
├── modules/Unity.Reporting/                          Reporting Configuration (module)
│   └── src/
│       ├── Unity.Reporting.Domain.Shared/            Providers, ViewStatus, RoleStatus, settings
│       ├── Unity.Reporting.Application.Contracts/    DTOs, IReportMappingService, permissions
│       ├── Unity.Reporting.Application/              ReportMappingService, ReportMappingUtils,
│       │                                             5 IFieldsProvider impls, background jobs,
│       │                                             ReportColumnsMap entity + repository + DbContext
│       └── Unity.Reporting.Web/                      ReportingConfiguration view component + controller
├── modules/Unity.Flex/src/Unity.Flex.Application/Reporting/Configuration/   worksheet/scoresheet field metadata
├── src/Unity.GrantManager.Application/Reporting/Configuration/              CHEFS form field metadata
└── src/Unity.GrantManager.EntityFrameworkCore/Scripts/         all SQL, deployed as embedded resources
```

All `Reporting` schema objects are created by the tenant migration `20260721203242_Initial`, which runs the `Scripts/*.sql` files as embedded resources. See `Scripts/README.md` for the embedded-resource contract.

## Handover pages

One-page visual summaries live in `documentation/handover/`:

- `reporting-configuration-handover.html` — Reporting Configuration
