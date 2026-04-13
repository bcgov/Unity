# Reporting Configuration

## Overview

The Reporting Configuration system allows administrators to define how source field data maps to PostgreSQL database columns for generated reporting views. It supports three correlation providers, each sourcing field metadata from a different system:

| Provider | Source | Correlation ID | Description |
|----------|--------|----------------|-------------|
| `formversion` | CHEFS form submissions | Form Version ID | Immutable form schema fields |
| `worksheet` | Unity.Flex worksheets | Form Version ID | Dynamic worksheet fields |
| `scoresheet` | Unity.Flex scoresheets | Form ID | Evaluation/scoring fields |

## Architecture

### Layered Components

- **`ReportMappingUtils`** (`Unity.Reporting.Application`) — Static utility methods for column name sanitization, validation, uniqueness enforcement, and mapping creation/update logic.
- **`ReportMappingService`** (`Unity.Reporting.Application`) — Application service orchestrating CRUD operations, field metadata retrieval, view generation, and provider resolution.
- **`IFieldsProvider`** (`Unity.Reporting.Application`) — Interface for correlation-specific field metadata extraction and change detection.
- **`ReportingConfigurationController`** (`Unity.Reporting.Web`) — MVC controller providing AJAX API endpoints for the UI.
- **`Default.js`** (`Unity.Reporting.Web`) — Client-side DataTable configuration, validation, and provider-aware UI logic.

### Field Providers

Each provider implements `IFieldsProvider`:

- **`FormVersionFieldsProvider`** — Retrieves field metadata from immutable CHEFS form version schemas. Change detection always returns null since form versions are immutable.
- **`WorksheetFieldsProvider`** — Retrieves field metadata from Unity.Flex worksheet definitions. Supports change detection for dynamic schema evolution.
- **`ScoresheetFieldsProvider`** — Retrieves field metadata from Unity.Flex scoresheet definitions. Supports change detection for scoring structure changes.

## Default Column Name Generation

When a user creates or saves a report configuration for the first time (or when new fields are discovered during an update), the system auto-generates default column names for fields that don't have user-specified names.

### Provider-Specific Column Name Source

The source used for generating default column names differs by provider:

| Provider | Default Column Name Source | Rationale |
|----------|---------------------------|-----------|
| `formversion` | **Key** (CHEFS Property Name, e.g., `firstName`) | CHEFS property names are stable, developer-defined identifiers that produce clean, predictable column names (e.g., `firstname`). Labels can be verbose or contain special characters. |
| `worksheet` | **Label** (e.g., `First Name`) | Worksheet labels are human-readable names configured by administrators, producing descriptive column names (e.g., `first_name`). |
| `scoresheet` | **Label** (e.g., `Score One`) | Scoresheet labels provide meaningful, user-facing descriptions that map well to reporting column names. |

### Column Name Sanitization

All auto-generated column names go through the same sanitization pipeline regardless of provider:

1. Convert to lowercase
2. Replace spaces and hyphens with underscores
3. Remove all non-alphanumeric characters (except underscores)
4. Remove consecutive underscores
5. Trim leading/trailing underscores
6. Prefix with `col_` if name starts with a digit
7. Truncate to 60 characters maximum
8. Ensure uniqueness by appending numeric suffixes (`_1`, `_2`, etc.) when collisions occur

### Implementation Details

The column name source selection is implemented in `ReportMappingUtils.GetDefaultColumnNameSource()`:

- **Server-side**: Used by `CreateNewMap()` and `UpdateExistingMap()` when auto-generating column names for unmapped or newly discovered fields.
- **Client-side**: The `getDefaultColumnNameSource()` function in `Default.js` mirrors this logic for the initial DataTable display when no saved configuration exists.

### Impact on Existing Configurations

- **Existing saved configurations are not affected.** Column names are persisted in the database; the provider-specific logic only determines defaults for new/unsaved fields.
- **Existing column names are preserved during updates.** The three-tier priority system (user-provided → existing → auto-generated) ensures established mappings are maintained.

## Column Name Validation

Both client-side and server-side enforce PostgreSQL column name rules:

- Maximum 60 characters
- Must start with a letter or underscore
- Contains only letters, digits, and underscores
- Cannot be a PostgreSQL reserved word
- Must be unique within the configuration

## View Generation

After saving a configuration, users can generate a PostgreSQL database view:

1. User provides a view name (validated for availability and PostgreSQL compliance)
2. A background job is queued to create/update the view in the `Reporting` schema
3. View status is tracked and displayed via a widget

View names follow similar sanitization rules with a 63-character maximum (PostgreSQL identifier limit).

## Configuration Lifecycle

```
Fields Metadata → [Save] → Configuration → [Generate View] → Database View
                                ↓
                          [Delete] → Removes configuration and optionally the view
```

1. **Initial Load**: Field metadata is fetched from the appropriate provider; default column names are generated.
2. **Save**: Creates or updates the mapping configuration with user-specified and auto-generated column names.
3. **Generate View**: Creates a PostgreSQL view in the `Reporting` schema based on the saved mapping.
4. **Delete**: Removes the configuration and optionally drops the associated database view.
