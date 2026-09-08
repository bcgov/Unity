# Flex DataGrid Field

`CustomFieldType.DataGrid` is a repeating-row grid field — by far the most complex of the fifteen usable field types, with its own column-population modes, a full CRUD editing surface, auto-sum, and dedicated reporting handling. It warrants its own reference rather than a row in the [field/question types table](flex-domain-model.md#field-types-and-question-types).

## Column population: three modes

A DataGrid's columns come from up to **two independent sources**, controlled by `DataGridDefinition.Dynamic` (bool) and `DataGridDefinition.Columns` (`List<DataGridDefinitionColumn>` — `Name`/`Type`/`Key`):

1. **Explicit** (`Dynamic = false`) — the admin declares `Columns` up front in the builder (`DataGridDefinitionWidget`), each with a unique name validated against an allow-listed character set. This is the only mode where a user can manually **add a brand-new row** at runtime.
2. **Dynamic** (`Dynamic = true`) — columns are not admin-declared. Instead, at CHEFS-intake-mapping time, `DynamicDataBuilder.BuildDataGrid` (`Unity.Flex.Shared/DynamicDataBuilder.cs`) walks the live CHEFS form schema, finds the matching `datagrid` component by key, reads its nested column sub-components (key, label, type — converted via `ChefsToUnityTypes.Convert`, with explicit date-vs-datetime disambiguation via the CHEFS `enableTime` flag), and builds the **value's own column list** (`DataGridValue.Columns` — distinct from `DataGridDefinition.Columns`) alongside the actual submitted row data. This is how a DataGrid's shape can track whatever columns a CHEFS form author configured, without the Flex admin declaring them separately.
3. **Combination** — a `Dynamic` grid can *also* carry explicitly-declared `DataGridDefinition.Columns` at the same time. Both the runtime widget (`DataGridWidget.GenerateDataColumns`) and the reporting-metadata parser (`WorksheetFieldSchemaParser.ParseDataGridField`) merge the two sources the same way: dynamic/CHEFS-sourced columns are emitted first (in CHEFS's own order), and explicit definition columns are appended for any key not already present — **dynamic wins on collision**. This lets one grid combine a variable, CHEFS-authored table with a handful of fixed, admin-added columns.

## Rendering & editing

- `DataGridWidget` renders the merged column set as an HTML table (`DataGridViewModel`), with a distinct preview mode (three placeholder variants — dynamic-with-no-columns, dynamic-with-columns, non-dynamic — driven by `UiAnchor == "Preview"`) so admins previewing an unpublished worksheet see a representative empty grid.
- **Editing is universal** — `AllowEdit` is always `true`. Every existing row is editable through `EditDataRowModal`, backed by:
  - `DataGridReadService` (`Unity.Flex.Web/Pages/Components/DataGrid/`) — loads a row's current values for the modal, handling three cases: first-row bootstrap (no data yet), new-row bootstrap, and editing an existing row.
  - `DataGridWriteService` — full CRUD: `AddFirstRowAsync` (creates a `WorksheetInstance` too, if none exists yet, via `IWorksheetInstanceAppService.CreateAsync`), `AddRowAsync`, `UpdateRowAsync`, `DeleteRowAsync`. Every write re-syncs the parent `WorksheetInstance`'s rolled-up value via `SyncWorksheetInstanceValueAsync`.
- **Adding a new row is restricted**: `GenerateAvailableTableOptions(!dataGridDefinition.Dynamic)` only offers "Add Record" when the grid is **not** dynamic. A fully or partially CHEFS-driven grid's column set isn't something a user can safely append an arbitrary new row to at runtime — only explicit, admin-declared grids support manual row insertion. Export (`ExportData`) is always available regardless.

## Column types & formatting

DataGrid columns support a curated subset of `CustomFieldType` (not all fifteen): **Text, TextArea, Currency, Numeric, Date, DateTime, YesNo, Checkbox, Phone, Email** (`DataGridDefinitionViewModel._supportedFieldTypes`).

`DataGridExtensions` (`Unity.Flex.Shared/`) applies per-column-type formatting in three directions:

- `ApplyPresentationFormatting` — for display (dates/currency/yes-no/checkbox formatted for reading).
- `ApplyStoreFormatting` — for persistence (normalized storage format).
- `ApplyInputFormatting` — for the editable input control, including timezone-offset correction for `DateTime` columns (browser offset applied via `PresentationSettings.BrowserOffsetMinutes`) and culture-aware currency formatting (ISO currency code → `CultureInfo` lookup).

## Auto-sum ("Total:") columns

- `DataGridDefinition.SummaryOption`: `None` / `Above` / `Below` — controls whether, and where, a summary row renders relative to the grid.
- Only **Numeric** and **Currency** columns are summable (`DataGridWidget._validTotalSummaryTypes`) — each gets its own `Total: {ColumnName}` summary field.
- `DataGridWidget.SumCells` sums every row's cell for that column as `decimal` (stripping `$` and `,` before parsing), clamped at `decimal.MaxValue` to avoid overflow rather than throwing.
- A dynamic grid with no resolvable columns yet shows a placeholder `Total: Dynamic` summary field in preview, since real column types aren't known until CHEFS data is actually mapped.

## Reporting behavior

DataGrid is the one field type where reporting treats dynamic and explicit columns **differently**, in `DataGridReportDataGenerator` (`Unity.Flex.Application/Reporting/DataGenerators/CustomFieldValueGenerators/`):

- **Explicitly-declared columns** become individual, named report fields — `{fieldKey}-{columnName}`, one per row, same as any other field.
- **Dynamic columns** (present in the value but *not* in `DataGridDefinition.Columns`) are **not** exploded into individual fields — `CaterForDynamicColumns` collapses all of them into a single combined JSON blob field, `{fieldKey}-DynamicColumns`. A variable, CHEFS-driven column set can't map onto fixed SQL columns, so it's kept as opaque JSON instead.
- The reporting-fields-metadata side (`WorksheetFieldSchemaParser.ParseDataGridField`) mirrors this split: if the dynamic columns can be resolved from the live CHEFS form schema (via a submission-header-mapping lookup keyed on `{field.Name}.DataGrid`), each becomes its own reporting component; otherwise a single `"Dynamic Columns"` placeholder component stands in for the whole unresolved set.

See [flex-application-services.md](flex-application-services.md#reporting-integration) for the broader reporting pipeline this feeds into — including a note on planned changes to how reporting views get generated.
