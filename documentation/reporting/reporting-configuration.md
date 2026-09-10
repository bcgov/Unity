# Reporting Configuration (Explicit Path)

> This is the **current, go-forward** way reporting views are created in Unity Portal. It is one of [two view-generation paths](README.md); the other — auto-generated "dynamic" views created on publish — is deprecated and documented in [reporting-auto-generated-views.md](reporting-auto-generated-views.md).

## Overview

The Reporting Configuration system allows administrators to define how source field data maps to PostgreSQL database columns for generated reporting views. It supports five correlation providers, each sourcing field metadata from a different system:

| Provider                   | Source                                | Correlation ID  | Description                                                |
| -------------------------- | ------------------------------------- | --------------- | ---------------------------------------------------------- |
| `formversion`              | CHEFS form submissions                | Form Version ID | Static field schema for a specific form version            |
| `formversion_consolidated` | CHEFS form submissions (all versions) | Form ID         | Merged submission fields across all versions of a form     |
| `worksheet`                | Unity.Flex worksheets                 | Form Version ID | Dynamic worksheet fields linked to a specific form version |
| `worksheet_consolidated`   | Unity.Flex worksheets (all versions)  | Form ID         | Merged worksheet fields across all versions of a form      |
| `scoresheet`               | Unity.Flex scoresheets                | Form ID         | Evaluation/scoring fields linked to a form                 |

> **Key distinction:** Per-version providers (`formversion`, `worksheet`) use a **Form Version ID** as the CorrelationId. Consolidated and scoresheet providers (`formversion_consolidated`, `worksheet_consolidated`, `scoresheet`) use the **Form ID**.

---

## Architecture

### Layered Components

- **`ReportMappingUtils`** (`Unity.Reporting.Application`) — Static utility methods for column name sanitization, validation, uniqueness enforcement, and mapping creation/update logic.
- **`ReportMappingService`** (`Unity.Reporting.Application`) — Application service orchestrating CRUD operations, field metadata retrieval, view generation, and provider resolution.
- **`IFieldsProvider`** (`Unity.Reporting.Application`) — Interface for correlation-specific field metadata extraction and change detection. Each provider registers itself by its `CorrelationProvider` string identifier.
- **`ReportingConfigurationController`** (`Unity.Reporting.Web`) — MVC controller providing AJAX API endpoints for the UI.
- **`Default.js`** (`Unity.Reporting.Web`) — Client-side DataTable configuration, validation, and provider-aware UI logic.

### Field Providers

Each provider implements `IFieldsProvider`:

- **`FormVersionFieldsProvider`** — Retrieves field metadata from immutable CHEFS form version schemas via `IFormMetadataService`. Change detection always returns null since form versions cannot change after creation.
- **`ConsolidatedFormVersionFieldsProvider`** — Retrieves and merges submission field metadata across **all versions** of a form (CorrelationId = Form ID). Change detection tracks added/removed form versions.
- **`WorksheetFieldsProvider`** — Retrieves field metadata from Unity.Flex worksheet definitions linked to a specific form version. Stamps duplicate DataPaths within a version with `(DK1)`, `(DK2)`, etc. to ensure uniqueness before returning. Change detection tracks worksheet link additions/removals.
- **`ConsolidatedWorksheetFieldsProvider`** — Retrieves and merges worksheet field metadata across **all versions** of a form (CorrelationId = Form ID). Applies the same DataPath uniqueification per version before merging. Change detection tracks added/removed form versions and worksheets within versions.
- **`ScoresheetFieldsProvider`** — Retrieves field metadata from the Unity.Flex scoresheet linked to a form via `IApplicationFormAppService`. CorrelationId is the Form ID. Change detection compares the stored scoresheet ID against the form's current scoresheet.

### Module layout

```
applications/Unity.GrantManager/modules/Unity.Reporting/src/
├── Unity.Reporting.Domain.Shared/         Providers, ViewStatus, RoleStatus, ReportingSettings, localization
├── Unity.Reporting.Application.Contracts/ DTOs, IReportMappingService, ITenantViewRoleAppService, ReportingPermissions
├── Unity.Reporting.Application/           ReportMappingService, ReportMappingUtils, TenantViewRoleAppService,
│                                          FieldsProviders/ (5), BackgroundJobs/ (2),
│                                          Domain/Configuration/ReportColumnsMap, EntityFrameworkCore/
└── Unity.Reporting.Web/                   ReportingConfiguration + ReportingConfigurationViewStatus view components,
                                           DatabaseInfoModal
```

The `ReportColumnsMaps` table lives in the `Reporting` schema and is owned by `ReportingDbContext` (`ReportingDbProperties`), mapped tenant-side. `ReportColumnsMap` is an `AuditedEntity<Guid>` implementing `IMultiTenant`, with `Mapping` stored as `jsonb`.

---

## Where it lives in the UI

The configuration screen is a view component, not a page of its own. It is rendered as the **Reporting Configuration** tab of the form configuration screen — `Unity.GrantManager.Web/Pages/ApplicationForms/Mapping.cshtml` — via `@await Component.InvokeAsync("ReportingConfiguration", new { formid = ... })`.

The tab is shown only when **both**:

- the `Unity.Reporting` feature is enabled for the tenant, and
- the current user has `Reporting.Configuration` (`ReportingPermissions.Configuration.Default`).

### Provider selection

`ReportingConfigurationViewComponent` defaults to **`formversion_consolidated`** when no provider is passed. The toolbar exposes a three-way toggle plus a mode sub-toggle:

| Toggle | Sub-toggle | Resulting provider | CorrelationId used |
| --- | --- | --- | --- |
| Submissions | Consolidated *(default)* | `formversion_consolidated` | Form ID |
| Submissions | Per Version | `formversion` | Selected form version ID |
| Worksheets | Consolidated | `worksheet_consolidated` | Form ID |
| Worksheets | Per Version | `worksheet` | Selected form version ID |
| Scoresheets | — | `scoresheet` | Form ID |

The form-version dropdown is visible only for the `formversion` provider (`IsVersionSelectorVisible`); for that provider the first available version is selected when none is supplied.

### Administrator actions

| Action | Behaviour |
| --- | --- |
| **Save** | Creates or updates the mapping (`Create` / `Update`). Requires `Reporting.Configuration.Update`. |
| **Generate View** | Opens a modal that validates the view name for availability and PostgreSQL compliance, then queues generation. Visible only once a configuration is saved. |
| **Delete** | Opens a confirmation modal listing what will be removed (the configuration, and the database view if one exists). Requires `Reporting.Configuration.Delete`. |
| **Undo** | Reverts unsaved edits in the table back to the last loaded state. |
| **Generate unique column names** | Re-runs the sanitisation/uniquing pipeline across all rows. |
| **Export / Import / Edit JSON** | Export downloads the current mapping as `column-mapping-{provider}-{correlationId}.json`; Import validates each entry has `propertyName` and `columnName` before applying; Edit opens the mapping in an inline JSON editor. All three operate on the in-memory table — nothing is persisted until Save. |
| **Edit description** | Sets `Mapping.Metadata.Description` (max 500 characters). |

Two inline warnings can appear above the table: a **duplicate keys** warning (the source schema produced `(DK1)`-prefixed paths, or two rows share a path) and a **dynamic columns** warning (a row with key `dynamic_columns`, meaning a data grid whose columns are populated from CHEFS at runtime).

A separate `ReportingConfigurationViewStatus` view component polls and displays the current `ViewName` / `ViewStatus`.

### HTTP endpoints

`ReportingConfigurationController` is routed at `/ReportingConfiguration` and serves the tab's AJAX calls:

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `Refresh` | Re-render the component for a different provider/version |
| GET | `GetConfiguration` | Load a saved mapping (with `DetectedChanges`) |
| GET | `Exists` | Whether a mapping exists for the correlation |
| GET | `GetFieldsMetadata` | Live field list from the provider |
| POST | `Create` / `Update` | Persist the mapping |
| GET | `IsViewNameAvailable` | Name availability (global or correlation-aware) |
| POST | `GenerateView` | Queue view generation |
| POST | `GenerateColumnNames` | Sanitised/unique column names for a key→label map |
| GET | `GetViewPreviewData` | Sample rows from the generated view |
| DELETE | `Delete` | Remove the mapping and drop its view |

---

## Consolidated Providers

The `formversion_consolidated` and `worksheet_consolidated` providers create a single unified view that spans all versions of a form. Because the CorrelationId is the Form ID (not a version ID), one mapping configuration covers the entire form history.

### Field Merging Logic

When multiple form versions each contribute fields, the provider merges them using the following rules based on (Label, Path, Type) matching:

| Scenario                                                          | Outcome                         | VersionLabel                                                    |
| ----------------------------------------------------------------- | ------------------------------- | --------------------------------------------------------------- |
| Field has identical (Label, Path, Type) across **all** versions   | Single merged row               | None (field is universal across all versions)                   |
| Field has identical (Label, Path, Type) in **some** versions only | Single row for that exact match | Comma-joined list of versions where it appears (e.g., `v1, v3`) |
| Same (Label, Path) but **different Type** across versions         | One row **per distinct type**   | Comma-joined list of versions carrying that specific type       |

The `VersionLabel` property on a field row is displayed in the mapping UI to help administrators understand which form versions contain that field. Fields with no VersionLabel are consistent across all versions.

### Change Detection for Consolidated Providers

- **`formversion_consolidated`** — A form version is added or removed. Tracked via `formversion_{versionId}` metadata keys stored in the mapping at save time.
- **`worksheet_consolidated`** — A form version is added/removed, or a worksheet is linked/unlinked within a version. Tracked via both `formversion_{versionId}` and `ws_{versionId}_{worksheetId}` metadata keys.

---

## Default Column Name Generation

When a user creates or saves a report configuration for the first time (or when new fields are discovered during an update), the system auto-generates default column names for fields that don't have user-specified names.

### Provider-Specific Column Name Source

The source used for generating default column names differs by provider:

- **`formversion`** — uses the field **Key** (CHEFS Property Name, e.g., `firstName`). CHEFS property names are stable, developer-defined identifiers that produce clean, predictable column names (e.g., `firstname`). Labels can be verbose or contain special characters.
- **`formversion_consolidated`** — uses the field **Label**. Consolidated views span multiple versions; labels are the primary human-readable identifiers across versions.
- **`worksheet`** — uses the field **Label**. Worksheet labels are human-readable names configured by administrators, producing descriptive column names (e.g., `first_name`).
- **`worksheet_consolidated`** — uses the field **Label**. Same rationale as `worksheet`.
- **`scoresheet`** — uses the field **Label**. Scoresheet labels provide meaningful, user-facing descriptions that map well to reporting column names.

> **Note:** Only the `formversion` provider uses the field Key as its default source. All other providers use the field Label. The `GetDefaultColumnNameSource()` method checks for an exact case-insensitive match against the string `"formversion"` — the consolidated variant `"formversion_consolidated"` does **not** match and therefore uses Label.

### Column Name Sanitization

All auto-generated column names go through the same sanitization pipeline regardless of provider:

1. Replace spaces and hyphens with underscores
2. Remove all characters that are not letters, digits, or underscores
3. Remove consecutive underscores (collapse `__` → `_`)
4. Trim leading/trailing underscores
5. Substitute `"col"` if nothing remains after trimming
6. Prefix with `col_` if the name starts with a digit
7. Truncate to 60 characters; trim any trailing underscore introduced by truncation
8. Convert to lowercase
9. Ensure uniqueness by appending numeric suffixes (`_1`, `_2`, etc.) when collisions occur — suffix length is accounted for during truncation to stay within the 60-character limit

### Implementation Details

The column name source selection is implemented identically on server and client with no cross-field fallbacks:

- **Server-side** — `ReportMappingUtils.GetDefaultColumnNameSource()` returns `field.Key ?? ""` for `formversion`, or `field.Label ?? ""` for all other providers. Used by `CreateNewMap()` and `UpdateExistingMap()` when auto-generating column names for unmapped or newly discovered fields. The provider comparison is case-insensitive via `StringComparison.OrdinalIgnoreCase`.
- **Client-side** — `getDefaultColumnNameSource()` in `Default.js` uses the same logic: `field.key || ''` when `currentProvider === 'formversion'`, otherwise `field.label || ''`. This is used by `transformFieldsMetadata()` to set initial column name values in the DataTable when no saved configuration exists.

> **Important:** Neither implementation falls back from Key to Label or vice versa. If the source value is null/empty, an empty string is used, and the downstream sanitization produces `"col_1"` as a placeholder (null/whitespace input returns `"col_1"` directly). This ensures client and server always produce identical defaults.

### Column Name Priority During Updates

When saving an updated configuration, column names are resolved using a three-tier priority system:

1. **User-provided** — a column name explicitly set by the administrator in the current save operation
2. **Existing** — the column name already persisted in the database for that field path
3. **Auto-generated** — a new sanitized name derived from the field Key or Label, generated only for fields not covered by tiers 1 or 2

This ensures established mappings are preserved and only genuinely new fields receive auto-generated names.

### Impact on Existing Configurations

- **Existing saved configurations are not affected.** Column names are persisted in the database; the provider-specific logic only determines defaults for new/unsaved fields.
- **Existing column names are preserved during updates.** The three-tier priority system ensures established mappings are maintained.

---

## Field Column Definitions

The configuration table displays one row per mappable field from the source. For the `worksheet` / `worksheet_consolidated` providers, **Worksheet Name** is the leftmost column. After that, the next three column headers change depending on the active provider tab; the remaining three are constant across all providers.

**Worksheet-only header (leftmost column):** **Worksheet Name** — shown only for the `worksheet` / `worksheet_consolidated` providers; absent (and every other column shifts one position left) for `formversion` / `formversion_consolidated` / `scoresheet`.

**Provider-specific headers (next three columns):**

- `formversion` / `formversion_consolidated` — **CHEFS Label**, **CHEFS Property Name**, **CHEFS Type**
- `worksheet` / `worksheet_consolidated` — **Worksheet Label**, **Worksheet Property Name**, **Worksheet Type**
- `scoresheet` — **Scoresheet Label**, **Scoresheet Property Name**, **Scoresheet Type**

**Constant headers (following three columns):** **Path**, **Report Column**, **Type Path**

**Source Order** — present for every provider as a trailing column, hidden by default (see below).

### Label *(CHEFS Label / Worksheet Label / Scoresheet Label)*

The human-readable display name as it appears in the source form or worksheet UI. For checkbox group options this is the individual option's label (e.g., `Urban`, `Rural`). Used as the default column name source for all providers except `formversion`.

### Property Name *(CHEFS Property Name / Worksheet Property Name / Scoresheet Property Name)*

The developer-assigned key from the source schema (e.g., `firstName`, `projectBudget`). Stable and unique within a form component — it does not change when the label is edited. For checkbox group options the property name is `{parentKey}-{optionValue}` (e.g., `sector-urban`). Used as the default column name source for the `formversion` provider.

### Type *(CHEFS Type / Worksheet Type / Scoresheet Type)*

The component type from Forms.io or Unity.Flex that determines how the raw data is stored and how the view generation SQL extracts it. Common values:

| Type | Description |
| ---- | ----------- |
| `textfield` / `textarea` | Free-text input — extracted as TEXT |
| `number` | Numeric input — extracted as NUMERIC |
| `currency` | Currency input — extracted as DECIMAL(18,2), locale commas stripped |
| `datetime` | Date/time input — extracted as TIMESTAMP by the worksheet providers only; the submission and scoresheet providers have no date branch and extract it as TEXT |
| `radio` | Single-select radio group — extracted as TEXT (the selected value) |
| `simplecheckboxes` | Multi-select checkbox group — each option becomes a separate row with type `option` |
| `checkbox` | Single boolean checkbox — extracted as BOOLEAN |
| `option` | An individual option within a checkbox group |
| `datagrid` | Repeating data grid — each row becomes a separate view row with a row identifier |

### Path *(UI column — displays DataPath)*

> **Important naming note:** The UI column labelled **"Path"** actually displays the `DataPath` field from the data model — not the internal `path` field. The internal `path` is stored in the mapping for row-identity purposes but is not shown as its own column.

The **Path** column shows the data-centric navigation path used by the PostgreSQL view generation SQL function to extract values from the JSON. Container segments that do not exist in the actual submitted JSON (panels, tabs, columns, fieldsets) are stripped from this path:

| Internal `path` (not displayed) | Internal `typePath` | Displayed "Path" (DataPath) | Why |
| -------------------------------- | ------------------- | --------------------------- | --- |
| `panel1->firstName` | `panel->textfield` | `firstName` | `panel` is a container — stripped |
| `tab1->firstName` | `tab->textfield` | `firstName` | `tab` is a container — stripped |
| `datagrid1->itemName` | `datagrid->textfield` | `datagrid1->itemName` | `datagrid` is kept — it IS in the JSON |
| `checkboxField->optionA` | `simplecheckboxes->option` | `checkboxField->optionA` | Both segments present in data |

The internal `path` (the full key-based breadcrumb including container ancestors) is stored in the mapping and used as the stable identity key when matching rows across saves and updates. If two fields resolve to the same key chain, duplicates are prefixed with `(DK1)`, `(DK2)`, etc. on both `path` and `dataPath`.

### Report Column

The user-editable PostgreSQL column name this field will occupy in the generated database view. Auto-generated from the field Property Name or Label (depending on provider) using the sanitization pipeline. Must be unique within the configuration and conform to PostgreSQL identifier rules (see [Column Name Validation](#column-name-validation)).

### Type Path

The same schema hierarchy as the internal `path` but using component types instead of keys (e.g., `panel->textfield`, `datagrid->textarea`). Not user-editable. Used internally by the view generation SQL function to determine how to navigate the JSON structure — for example, a `datagrid` segment signals that `jsonb_array_elements` is needed. Also used to identify which segments are containers when computing the DataPath.

### Version Label *(consolidated providers only)*

Indicates which form versions contain this field. Only present when using `formversion_consolidated` or `worksheet_consolidated`:

- Blank — field is present in all versions with the same type; merged into a single column
- `v1` — field exists only in version 1
- `v1, v3` — field exists in versions 1 and 3 but not in all versions

### Worksheet Name *(worksheet providers only)*

The name of the source `Worksheet` this field came from, including its version suffix (e.g., `grant_application-v2`). Only present for the `worksheet` and `worksheet_consolidated` providers; blank for `formversion` and `scoresheet`.

### Source Order

A numeric column, hidden by default, giving each field's 1-based position in the provider's calculated overall field order. Present for every provider. Not user-editable — for saved configurations it reflects the value stored in the mapping (older mappings are backfilled from live provider metadata) and is not affected by saved Report Column values. Users can reveal it via the column picker to inspect the underlying order, or to manually re-sort back to it after sorting by another column.

### Default Sort Order

For the `worksheet` and `worksheet_consolidated` providers, rows are returned pre-sorted (and numbered via **Source Order**) by:

1. **Worksheet Name**, A–Z (the version suffix means later versions naturally sort after earlier ones)
2. **Section order** within each worksheet
3. **Field layout order** within each section (top to bottom, left to right)
4. **Checkbox group option order** — when a single Checkbox Group field expands into one row per option, the rows follow the order the options are defined on the field (not alphabetical)
5. **Data grid column order** — when a single Data Grid field expands into one row per column, the rows follow the order the columns are defined on the grid. For a mixed dynamic/static grid, CHEFS-extracted dynamic columns are emitted first (in CHEFS's own order), followed by any additional statically-defined columns not already covered

For the `worksheet` and `worksheet_consolidated` providers, the table's default sort is by **Source Order**, ascending, until the user explicitly sorts by another column, at which point the existing sort/column-visibility persistence takes over. Other providers (`formversion`, `formversion_consolidated`, `scoresheet`) continue to default-sort by Label, unchanged — their **Source Order** column is still populated (reflecting each provider's natural field order) and available via the column picker, but is not the default sort key.

---

## Column Name Validation

Both client-side and server-side enforce PostgreSQL column name rules:

- Maximum 60 characters
- Must start with a letter or underscore (not a digit)
- Contains only letters, digits, and underscores
- Cannot be a PostgreSQL reserved word (enforced against a comprehensive built-in list)
- Must be unique within the configuration

---

## View Generation

After saving a configuration, users can generate a PostgreSQL database view:

1. User provides a view name (validated for availability and PostgreSQL compliance)
2. The service normalises the view name to lowercase and checks availability for the given correlation
3. The mapping's `ViewStatus` is set to `GENERATING` and persisted
4. A `GenerateViewBackgroundJob` is queued to perform the actual view creation asynchronously
5. The background job runs within the correct tenant context; if the view name has changed, the old view is deleted first
6. On success, `ViewStatus` is updated to `SUCCESS` and a second job (`AssignViewRoleBackgroundJob`) is automatically queued to grant `SELECT` on the new view to the tenant's configured reporting role
7. On failure, `ViewStatus` is set to `FAILED` and the error is logged
8. View status is polled and displayed to the administrator via a widget

View names follow similar sanitization rules with a 63-character maximum (PostgreSQL identifier limit).

### Provider → stored procedure routing

`ReportColumnsMapRepository.GenerateViewAsync` dispatches on the correlation provider; an unrecognised provider throws `ArgumentException`.

| Provider | Procedure | `SELECT` built by |
| --- | --- | --- |
| `formversion` | `Reporting.generate_formversion_view(uuid)` | `Reporting.get_formversion_data(correlation_id, report_map_id)` |
| `formversion_consolidated` | `Reporting.generate_consolidated_formversion_view(uuid)` | `Reporting.get_consolidated_formversion_data(...)` |
| `worksheet` | `Reporting.generate_worksheet_view(uuid)` | `Reporting.get_worksheet_data(...)` |
| `worksheet_consolidated` | `Reporting.generate_consolidated_worksheet_view(uuid)` | `Reporting.get_consolidated_worksheet_data(...)` |
| `scoresheet` | `Reporting.generate_scoresheet_view(uuid)` | `Reporting.get_scoresheet_data(...)` |

Each procedure reads its mapping row out of `Reporting."ReportColumnsMaps"`, raises an exception if the mapping or its `Rows` array is missing, asks the matching `get_*_data` function for a complete `SELECT` statement, then `DROP VIEW IF EXISTS "Reporting".{name}` followed by `CREATE VIEW "Reporting".{name} AS {statement}`.

**Where the values come from at query time** — the generated views read source data directly; they do not read any pre-flattened snapshot:

| Provider | Source |
| --- | --- |
| `formversion`, `formversion_consolidated` | `public."ApplicationFormSubmissions"."Submission"` (raw CHEFS JSON, handling both the legacy and current `submission->submission` shapes) |
| `worksheet`, `worksheet_consolidated` | `Flex."WorksheetInstances"."CurrentValue"` joined to `Flex."Worksheets"` |
| `scoresheet` | `Flex."ScoresheetInstances"` → `Assessments` → `Applications`, values from `Flex."Answers"` / `Flex."Questions"`, plus `total_score` from `Reporting.calculate_scoresheet_total_score(instance_id)` |

**Typing is not uniform across providers.** Only the two worksheet functions use the `Reporting.safe_to_date` / `safe_to_timestamp` / `safe_to_jsonb` helpers, and only they emit `TIMESTAMP` columns:

| | Worksheet (`worksheet`, `worksheet_consolidated`) | Submissions (`formversion`, `formversion_consolidated`) | `scoresheet` |
| --- | --- | --- | --- |
| Typed columns | `TEXT`, `NUMERIC`, `DECIMAL(18,2)`, `TIMESTAMP`, `BOOLEAN` | `TEXT`, `NUMERIC`, `DECIMAL(18,2)`, `BOOLEAN` | `TEXT`, `NUMERIC`, `BOOLEAN` |
| Date/time handling | `safe_to_date` / `safe_to_timestamp` | none — dates land as `TEXT` | none — dates land as `TEXT` |
| `safe_to_jsonb` guard | yes (checkbox groups) | no | no |
| Type-conflict fallback | n/a | `formversion` only: whole column falls back to `TEXT` | yes, falls back to `TEXT` |

Where the helpers are used, a malformed value yields `NULL` rather than failing the whole view. Per-type SQL is documented in the five `get_*_data_specification.md` files.

### Role Assignment

Generated views are useless to Metabase until a database role can read them. Role assignment is per tenant and is stored as the ABP setting `GrantManager.Reporting.TenantViewRole` at the tenant (`"T"`) scope. The host-level `GrantManager.Reporting.ViewRole` setting is deprecated and retained only for backward compatibility.

- `AssignViewRoleBackgroundJob` runs in the tenant context, reads the setting, verifies the role exists in `pg_roles`, then grants `SELECT` — on one view when `ViewName` is supplied (the automatic queue after a successful generation), or on **every** view in the `Reporting` schema when it is not.
- `TenantViewRoleAppService` (`[Authorize(IdentityConsts.ITAdminPermissionName)]`) reads, updates, and manually re-runs assignment. When no role has been saved it infers one: `{LicencePlate}_readonly` from the tenant's `LicencePlate` extra property, falling back to `{tenantname}_readonly`. `UpdateAsync` and `AssignRoleToViewsAsync` both fail fast with a `UserFriendlyException` if the role does not exist in the tenant's database — the background job alone would only log a warning and silently no-op.
- `GetTenantDatabaseInfoAsync` backs the **View DB Info** modal (`DatabaseInfoModal`), listing the tenant's database roles, role memberships, and all views currently in the `Reporting` schema.

> **Interaction with the deprecated Auto path:** `AssignRoleToAllViewsAsync` validates every view name it reads back from `pg_views` against `^[a-zA-Z_][a-zA-Z0-9_]*$` before interpolating it into the `GRANT`. Auto-generated view names (`Form-…`, `Worksheet-…`, `Scoresheet-…`) contain hyphens and fail that check, so the grant loop throws part-way through on any database that still holds them. See [reporting-auto-generated-views.md](reporting-auto-generated-views.md#known-rough-edges).

### View Name Availability

Two availability checks exist:

- **Global check** (`IsViewNameAvailableAsync(viewName)`) — Returns true if no view with that name exists in the Reporting schema. Used for name suggestions before a configuration is saved.
- **Correlation-aware check** (`IsViewNameAvailableAsync(viewName, correlationId, correlationProvider)`) — Returns true if no view with that name exists, **or** if the existing view belongs to the same correlation. This allows regenerating a view under the same name without a false conflict error.

---

## Change Detection

When an existing configuration is loaded, the provider's `DetectChangesAsync` method compares the current state of the source data against what was stored in the mapping's metadata at the time of the last save. Detected changes are surfaced as a human-readable description string in `ReportColumnsMapDto.DetectedChanges`, which the UI displays as an alert to the administrator.

- **`formversion`** — No change detection. Form versions are immutable by design; always returns null.
- **`formversion_consolidated`** — Compares stored `formversion_{versionId}` metadata keys against the current list of form versions with fields.
- **`worksheet`** — Compares stored `worksheet_{worksheetId}` metadata keys against the current list of worksheet links for the form version.
- **`worksheet_consolidated`** — Compares stored `formversion_{versionId}` and `ws_{versionId}_{worksheetId}` metadata keys against current worksheet links across all versions.
- **`scoresheet`** — Compares stored `scoresheet_{scoresheetId}` metadata key against the form's current scoresheet assignment.

---

## Configuration Lifecycle

```text
Fields Metadata → [Save] → Configuration → [Generate View] → Database View
                                ↓                                   ↓
                          [Delete]                         [Assign Role Job]
                      Removes config                      Grants SELECT to
                    and drops the view                    reporting role
```

1. **Initial Load** — Field metadata is fetched from the appropriate provider; default column names are generated client-side for display.
2. **Save** — Creates or updates the mapping configuration in the database with user-specified and auto-generated column names.
3. **Generate View** — Queues a background job that creates or replaces a PostgreSQL view in the `Reporting` schema based on the saved mapping. Role assignment is queued automatically on success.
4. **Delete** — Removes the mapping configuration record and drops the associated database view if it exists. View deletion failure is logged but does not prevent the mapping from being removed.
