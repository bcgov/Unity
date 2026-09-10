# Reporting Architecture

## Overview

The Unity Grant Manager reporting stack is built in discrete, purposeful layers. Each layer abstracts and enriches the one beneath it, so data moves from raw storage through structured views, into curated semantic models, and finally into specific visualisations or exports — with clear boundaries and responsibilities at every step.

```
Raw Database  →  Views (Reporting schema)  →  Metabase Models  →  Cards / Questions
   (source)         (structured, flat)         (shared context)     (specific outputs)
```

Any reporting tool — not just Metabase — can be pointed at the raw database and generated views to build its own layer on top. Metabase is our current choice, not a hard dependency.

## Two paths into Layer 2

Layer 2 — the views — is produced by **two independent mechanisms** that both write into the `Reporting` schema. A given database may contain views from both.

| | **Explicit** — Reporting Configuration | **Auto / Dynamic** — legacy |
| --- | --- | --- |
| Status | **Current, go-forward** | **Deprecated — scheduled for removal** |
| Who decides a view exists | An administrator, on the Reporting Configuration tab | Nobody — one is created automatically on publish |
| Configuration store | `Reporting.ReportColumnsMaps` | `ReportKeys` / `ReportColumns` / `ReportViewName` on the definition row |
| Column types | `NUMERIC`, `DECIMAL(18,2)`, `TIMESTAMP`, `BOOLEAN`, `TEXT` | `TEXT` for everything (plus one `integer` `TotalScore`) |
| Value source at query time | Source data directly — `Submission`, `WorksheetInstances.CurrentValue`, `Flex.Answers` | The pre-flattened `ReportData` JSONB snapshot |
| Cross-version views | Yes (`*_consolidated` providers) | No — one view per version / worksheet / scoresheet |
| Detail | [reporting-configuration.md](reporting-configuration.md) | [reporting-auto-generated-views.md](reporting-auto-generated-views.md) |

The two do not interfere at the data level — the explicit path's SQL never reads `ReportData` or the `Report*` definition columns — so the Auto path can be switched off without touching any explicitly configured view. The rest of this document describes the layer model; where a layer differs between the paths, it says so.

---

## Architecture Layers

```mermaid
flowchart TD
    subgraph DB["PostgreSQL Database"]
        direction TB
        RAW["🗄️ Raw Tables\n(Applications, JSON form data,\nworksheet records, scoresheet records)"]
        RSCHEMA["📐 Reporting Schema\n(Generated Views — flattened JSON)"]
    end

    subgraph APP["Unity.GrantManager Application"]
        CFG["⚙️ Reporting Configuration\n(field mapping + view generation)"]
        AUTO["⛔ Auto/Dynamic generators\nDEPRECATED — one view per\nform version / worksheet / scoresheet"]
        CHEFS["📋 CHEFS Form Submissions\nformversion / formversion_consolidated"]
        WS["📝 Unity.Flex Worksheets\nworksheet / worksheet_consolidated"]
        SS["🎯 Unity.Flex Scoresheets\nscoresheet"]
    end

    subgraph MB["Metabase (External BI Tool)"]
        MODELS["📦 Metabase Models\n(curated, shared semantic sources)"]
        CARDS["📊 Cards & Dashboards\n(specific questions & visualisations)"]
    end

    ALT["🔌 Alternative Reporting Tools\n(Power BI, Tableau, custom tools, etc.)"]

    RAW -->|"raw columns available\ndirectly"| MB
    RAW -->|"source JSON\ndigested by"| CFG
    CFG -->|"generates stored views\nvia background job"| RSCHEMA
    AUTO -.->|"auto-generates TEXT-only\nviews on publish"| RSCHEMA
    CHEFS --> CFG
    WS --> CFG
    SS --> CFG
    CHEFS -.-> AUTO
    WS -.-> AUTO
    SS -.-> AUTO
    RSCHEMA -->|"flat, named columns\nready to consume"| MODELS
    RAW -->|"raw columns\n(for models that need them)"| MODELS
    MODELS -->|"shared semantic layer\nqueried by"| CARDS
    RSCHEMA -->|"views exposed to"| ALT
    RAW -->|"raw tables exposed to"| ALT

    style DB fill:#e8f4f8,stroke:#2980b9
    style APP fill:#eafaf1,stroke:#27ae60
    style MB fill:#fef9e7,stroke:#f39c12
    style ALT fill:#f9ebea,stroke:#e74c3c
```

---

## Layer 1 — Raw Database

**What it is:** The PostgreSQL database that the application writes to directly. This is the authoritative source of record.

**What it contains:**
- Application records, applicant details, statuses, assignments, dates
- Form submission data stored as **JSON blobs** inside columns (CHEFS submissions)
- Worksheet field values (Unity.Flex) — current state
- Scoresheet field values (Unity.Flex) — current state

**Key characteristic:** Form submission JSON is opaque to standard SQL queries. A tool cannot easily filter or aggregate on individual form fields without first understanding the schema — that is what the next layer solves.

**Access:** Any database role with appropriate `SELECT` grants can read raw tables directly. Metabase models (or any other tool) may source from here when they need columns that are already structured (e.g., `ApplicationId`, `Status`, dates).

---

## Layer 2 — Views (Reporting Schema)

**What it is:** PostgreSQL views in the `Reporting` schema. These views do the hard work of extracting individual fields out of the JSON and presenting them as ordinary, flat, named columns.

Two mechanisms create them — see [Two paths into Layer 2](#two-paths-into-layer-2). The rest of this section describes the **explicit** Reporting Configuration path, which is the one to build on. The deprecated Auto path is summarised in [Layer 2b](#layer-2b--auto-generated-views-deprecated) below and documented in full in [reporting-auto-generated-views.md](reporting-auto-generated-views.md).

**How they are created:**

```mermaid
sequenceDiagram
    participant Admin
    participant Config as Reporting Configuration UI
    participant Service as ReportMappingService
    participant Job as Background Job
    participant DB as PostgreSQL (Reporting schema)

    Admin->>Config: Select source (form version / worksheet / scoresheet)
    Config->>Service: Fetch field metadata from provider
    Service-->>Config: Field list (keys, labels, paths)
    Admin->>Config: Map fields → column names, save
    Admin->>Config: Click "Generate View"
    Config->>Service: GenerateViewAsync(correlationId, provider, viewName)
    Service->>Job: Enqueue GenerateViewBackgroundJob
    Job->>DB: CALL Reporting.generate_*_view(correlationId)
    DB-->>Job: View created/updated
    Job->>DB: Update ViewStatus → SUCCESS
    Job->>RoleJob: Enqueue AssignViewRoleBackgroundJob
    RoleJob->>DB: GRANT SELECT ON view TO reporting_role
```

**Provider types and data characteristics:**

| Provider | Correlation ID | Data Nature | Use Case |
|----------|---------------|-------------|----------|
| `formversion` | Form Version ID | **Point-in-time / static** — immutable snapshot of a specific CHEFS form version's submission data | Reporting on what applicants submitted for a given form version |
| `formversion_consolidated` | Form ID | **Point-in-time / static** — merged across all versions of a form | Cross-version submission reporting for a single form |
| `worksheet` | Form Version ID | **Current state** — live worksheet field values as they exist today | Reporting on current worksheet data linked to a form version |
| `worksheet_consolidated` | Form ID | **Current state** — merged across all versions | Cross-version worksheet reporting |
| `scoresheet` | Form ID | **Current state** — live evaluation/scoring data | Reporting on assessor scores and evaluation criteria |

> **Important distinction:** `formversion` views capture what was submitted and do not change retroactively. `worksheet` and `scoresheet` views reflect the current state of those records, meaning the view data can change as worksheets and scoresheets are updated.

**What a view looks like:**

Before the view:
```sql
-- Raw: JSON blob, not queryable field-by-field
SELECT submission_data FROM Applications WHERE ...
-- Result: {"firstName":"Jane","projectBudget":50000,"region":"North"}
```

After the view:
```sql
-- View: flat, named columns
SELECT application_id, first_name, project_budget, region
FROM "Reporting"."my_form_v2_submissions"
WHERE region = 'North'
```

**Schema and naming:**
- All views live in the `Reporting` PostgreSQL schema
- View names and column names are sanitised to valid PostgreSQL identifiers (lowercase, underscores, max 63 chars)
- Column names are auto-generated from field keys or labels depending on provider, and can be overridden by administrators
- A database role is granted `SELECT` on each view after generation, which is what Metabase (or any other tool) uses to connect

**Provider → stored procedure routing** (`ReportColumnsMapRepository.GenerateViewAsync`):

| Provider | Procedure called | Which builds its `SELECT` from |
| --- | --- | --- |
| `formversion` | `Reporting.generate_formversion_view(uuid)` | `Reporting.get_formversion_data(correlation_id, report_map_id)` |
| `formversion_consolidated` | `Reporting.generate_consolidated_formversion_view(uuid)` | `Reporting.get_consolidated_formversion_data(...)` |
| `worksheet` | `Reporting.generate_worksheet_view(uuid)` | `Reporting.get_worksheet_data(...)` |
| `worksheet_consolidated` | `Reporting.generate_consolidated_worksheet_view(uuid)` | `Reporting.get_consolidated_worksheet_data(...)` |
| `scoresheet` | `Reporting.generate_scoresheet_view(uuid)` | `Reporting.get_scoresheet_data(...)` |

Each `generate_*` procedure reads the mapping row out of `Reporting."ReportColumnsMaps"`, asks the matching `get_*_data` function for a complete `SELECT` statement, drops any existing view of that name, and creates the view from that statement.

Column typing varies by provider: all five emit `TEXT` / `NUMERIC` / `DECIMAL(18,2)` / `BOOLEAN`, but only the two worksheet functions use the `Reporting.safe_to_date` / `safe_to_timestamp` / `safe_to_jsonb` helpers and only they emit `TIMESTAMP` — under the submission and scoresheet providers, date fields land as `TEXT`. The scoresheet views additionally call `Reporting.calculate_scoresheet_total_score`. See [reporting-configuration.md](reporting-configuration.md#provider--stored-procedure-routing) for the full comparison.

---

## Layer 2b — Auto-generated views (deprecated)

**What it is:** A second, older mechanism that creates one `Reporting` view per CHEFS form version, per published worksheet, and per published scoresheet — with no configuration step. It reads a pipe-delimited key/column list stored on the definition row and emits one `TEXT` column per key, each a lookup into a pre-flattened `ReportData` JSONB snapshot on the instance row.

| Source | View name pattern | Procedure |
| --- | --- | --- |
| CHEFS form version | `Form-{FormName}-V{Version}` | `Reporting.generate_submissions_view(uuid)` |
| Flex worksheet | `Worksheet-{WorksheetName}` | `Reporting.generate_worksheets_view(uuid)` |
| Flex scoresheet | `Scoresheet-{ScoresheetName}` | `Reporting.generate_scoresheets_view(uuid)` |

**Why it is being retired:** every column is `TEXT`; view names contain hyphens and so are not valid bare PostgreSQL identifiers (which breaks the "assign role to all views" operation); column names are truncated at 63 characters without de-duplication; there is no cross-version view; generation failures are logged and otherwise silent; and `ReportData` is a duplicate copy of data that already exists in the source tables.

**How to tell them apart in a database:** the explicitly configured views are exactly the `ViewName` values in `Reporting."ReportColumnsMaps"`. Anything else under `Reporting` in `pg_views` is an auto view or an orphan.

Full detail, including the phased removal plan, is in [reporting-auto-generated-views.md](reporting-auto-generated-views.md).

---

## Layer 3 — Metabase Models

**What it is:** Metabase **Models** are curated, shared data sources defined inside Metabase. They sit between the raw data / views and the end-user questions, acting as a semantic layer.

**What they solve:** Views expose the right columns, but they don't carry business meaning on their own. A model adds:
- Friendly field names and descriptions visible to all report authors
- Implicit joins between related sources (e.g., linking a form submission view to application metadata)
- Pre-applied filters or transformations that should be consistent across all downstream reports
- A single trusted definition that multiple cards can reference — change the model, and all cards using it update automatically

**Source options for a model:**

```mermaid
flowchart LR
    RAW["Raw Table\n(e.g., Applications)"]
    VIEW["Reporting View\n(e.g., my_form_submissions)"]
    MODEL["Metabase Model\n(e.g., Grant Applications — Full)"]

    RAW -->|structured columns| MODEL
    VIEW -->|flattened form fields| MODEL
```

A model may combine a raw table (for structured application columns) with one or more reporting views (for form field columns) through a join — giving report authors a single, unified source.

**Examples:**

| Model Name | Sources | Purpose |
|------------|---------|---------|
| `Grant Applications — Core` | Raw `Applications` table | Status, dates, applicant, assignments |
| `Application Form Responses — FY2024` | `formversion` view | Flattened CHEFS submission fields for a specific form version |
| `Assessment Scoresheets` | `scoresheet` view + `Applications` raw | Evaluation scores joined to application metadata |
| `Worksheet Summary` | `worksheet_consolidated` view | All worksheet responses across form versions |

---

## Layer 4 — Cards, Questions & Dashboards

**What it is:** The specific reports, charts, tables, and dashboards that end users see and use. In Metabase these are called **Cards** (saved questions) and **Dashboards** (collections of cards).

**Key characteristic:** Cards are narrow and specific. They answer one question ("How many applications by region this quarter?") by querying a model. Because cards inherit from models, the underlying data logic is not duplicated in every card.

**Types:**
- **No-code questions** — built using Metabase's visual query builder on top of a model; no SQL required
- **Native SQL questions** — custom SQL queries written directly against the database (can reference views or raw tables by name)
- **Dashboards** — assembled from multiple cards, often with filters and parameters

---

## Full Abstraction Flow

```mermaid
flowchart LR
    J["JSON in\nraw columns"]
    V["Named columns\nin Reporting view"]
    M["Shared model\nin Metabase"]
    C["Specific card\nor dashboard"]

    J -->|"Reporting\nConfiguration\ngenerates view"| V
    V -->|"Metabase admin\ndefines model"| M
    M -->|"Report author\nbuilds card"| C

    style J fill:#fdecea,stroke:#e74c3c
    style V fill:#e8f4f8,stroke:#2980b9
    style M fill:#eafaf1,stroke:#27ae60
    style C fill:#fef9e7,stroke:#f39c12
```

| Step | Who does it | What changes |
|------|-------------|--------------|
| Raw → View | Application administrator (Reporting Configuration UI) | Unstructured JSON → flat, queryable SQL columns |
| View → Model | Metabase administrator | Flat columns → named, joined, business-meaningful source |
| Model → Card | Report author | Abstract source → specific question / visualisation |

Each layer adds meaning without duplicating data. The raw database is the single source of truth; everything above it is a structured lens on top.

---

## Tool Independence

Metabase is the current BI tool, but the architecture does not depend on it. The `Reporting` schema views are standard PostgreSQL views, accessible to any tool that can connect to the database with the reporting role.

```mermaid
flowchart TD
    DB["PostgreSQL\n(Raw Tables + Reporting Views)"]

    MB["Metabase\n(current)"]
    PBI["Power BI"]
    TAB["Tableau"]
    CUSTOM["Custom Application\n/ API"]

    DB -->|"reporting role\nSELECT grant"| MB
    DB -->|"reporting role\nSELECT grant"| PBI
    DB -->|"reporting role\nSELECT grant"| TAB
    DB -->|"reporting role\nSELECT grant"| CUSTOM
```

A team adopting a different tool would:
1. Connect the tool to the PostgreSQL database using the reporting role credentials
2. Point to the `Reporting` schema views for form/worksheet/scoresheet field data
3. Point to raw tables for structured application metadata
4. Rebuild the semantic / model layer within that tool's conventions

The view generation system (Reporting Configuration) remains the same regardless of which tool sits above it.

---

## Use Cases

### Use Case 1 — Form Submission Reporting (Point-in-Time)

> *"Show me all applications submitted under Form Version 3, with the project budget and region fields."*

- **Source:** `formversion` view generated from CHEFS form version 3's schema
- **Data nature:** Static — reflects what applicants submitted; does not change even if the form is updated later
- **Path:** Raw JSON submission → Reporting Configuration maps fields → view generated in `Reporting` schema → Metabase model joins view with application metadata → card filters and displays

---

### Use Case 2 — Worksheet Progress Tracking (Current State)

> *"What is the current completion status of worksheets for all open applications?"*

- **Source:** `worksheet` or `worksheet_consolidated` view
- **Data nature:** Current — reflects the live state of worksheet data as assessors fill it in
- **Path:** Unity.Flex worksheet records → Reporting Configuration maps fields → view generated → Metabase model → dashboard card refreshes to show latest state

---

### Use Case 3 — Scoresheet Evaluation Summary (Current State)

> *"What are the average scores per criterion across all applications in this intake?"*

- **Source:** `scoresheet` view joined to `Applications` raw table
- **Data nature:** Current — reflects assessor scores at query time
- **Path:** Unity.Flex scoresheet records → view generated → Metabase model with join → aggregation card on dashboard

---

### Use Case 4 — Cross-Version Consolidated Reporting

> *"Aggregate project budget across all versions of Form X, even though different versions had different field names."*

- **Source:** `formversion_consolidated` view (CorrelationId = FormId, not a specific version)
- **Data nature:** Static per submission, but the view covers all versions
- **Path:** Reporting Configuration generates a consolidated stored procedure (`generate_consolidated_formversion_view`) → single view normalises fields across versions → Metabase model → report

---

### Use Case 5 — Custom SQL Report (Direct View Access)

> *"Write a custom SQL query joining form fields with application dates for an ad-hoc data export."*

- **Source:** Directly queries `Reporting` schema view by name in a Metabase native SQL question or any other SQL client
- No model required — the view itself is the queryable surface
- Example:
  ```sql
  SELECT
      v.application_id,
      v.project_budget,
      v.region,
      a."Status",
      a."SubmissionDate"
  FROM "Reporting"."my_form_v2_submissions" v
  JOIN "Applications" a ON v.application_id = a."Id"
  WHERE a."Status" = 'Approved'
  ORDER BY v.project_budget DESC
  ```

---

### Use Case 6 — Alternative Tool Integration

> *"We want to use Power BI instead of Metabase."*

- Connect Power BI to PostgreSQL using the reporting role
- Import or DirectQuery against `Reporting` schema views (for form/worksheet/scoresheet fields)
- Import or DirectQuery against raw tables (for application metadata)
- Build Power BI datasets (equivalent of Metabase models) on top
- Views remain unchanged — the Reporting Configuration system generates and maintains them regardless of which tool reads them

---

## Summary

| Layer | Technology | Managed by | Data shape |
|-------|-----------|-----------|------------|
| Raw Database | PostgreSQL tables | Application writes | Normalised rows + JSON blobs |
| Reporting Views | PostgreSQL views (`Reporting` schema) | Reporting Configuration UI | Flat, named, **typed** columns |
| Reporting Views *(deprecated)* | PostgreSQL views (`Reporting` schema) | Auto-generated on publish — no owner | Flat, named, **all `TEXT`** columns |
| Models | Metabase Models | Metabase authors | Named, joined, business-labelled sources |
| Cards / Dashboards | Metabase Questions & Dashboards | Report authors | Specific questions and visualisations |

Layers 3 and 4 are identical regardless of which mechanism produced the view underneath — which is what makes the Auto path's removal a migration of Metabase sources rather than a rebuild of the reports themselves.
