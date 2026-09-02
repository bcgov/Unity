# Flex Application Services

## App service APIs

### Worksheets (`Unity.Flex.Application.Contracts/Worksheets/`)

- **`IWorksheetAppService`** — `GetAsync`, `GetListAsync`, `GetListByCorrelationAsync(correlationId, correlationProvider)`, `GetListByCorrelationAnchorAsync(correlationId, correlationProvider, uiAnchor)`, `CreateAsync`, `CreateSectionAsync`, `EditAsync`, `CloneAsync`, `PublishAsync`, `ArchiveAsync(id, archive)`, `DeleteAsync`, `GetLinkedFormsAsync`, `ResequenceSectionsAsync`, `ExistsAsync`, `ExportWorksheet` / `ImportWorksheetAsync` (JSON import/export of a whole worksheet).
- **`IWorksheetListAppService`** — lightweight read-only lookups (`GetAsync`, `GetListByCorrelationAsync`, `GetListAsync`) returning `WorksheetBasicDto`.
- **`IWorksheetSectionAppService`** — `CreateCustomFieldAsync`, `ResequenceCustomFieldsAsync`, `GetAsync`, `EditAsync`, `DeleteAsync`.
- **`ICustomFieldAppService`** — `GetAsync`, `EditAsync`, `DeleteAsync`, `MoveToSectionAsync(fieldId, targetSectionId, newIndex)`.
- **`ICustomFieldValueAppService`** (`WorksheetInstances/`) — public `GetAsync`; internal-only (`[RemoteService(false)]`, not exposed over HTTP) `ExplicitSetAsync`, `ExplicitAddAsync`, `SyncWorksheetInstanceValueAsync`.
- **`IWorksheetLinkAppService`** (`WorksheetLinks/`) — `UpdateWorksheetLinksAsync`, `GetListByCorrelationAsync`, `GetListByWorksheetAsync`. Links a worksheet to an external entity via `(correlationId, correlationProvider)` (`Unity.Modules.Shared.Correlation.CorrelationConsts`, e.g. `CorrelationConsts.Application`, `CorrelationConsts.FormVersion`).

### Scoresheets (`Unity.Flex.Application.Contracts/Scoresheets/`)

- **`IScoresheetAppService`** — `CreateAsync`, `CreateQuestionInHighestOrderSectionAsync`, `CreateSectionAsync`, `DeleteAsync`, `CloneScoresheetAsync`, `GetAsync`, `GetListAsync`, `GetAllPublishedScoresheetsAsync`, `SaveOrder` / `SaveScoresheetOrder`, `UpdateAsync`, `GetNumericQuestionIdsAsync`, `GetYesNoQuestionsAsync`, `GetSelectListQuestionsAsync`, `ValidateChangeableScoresheet`, `PublishScoresheetAsync`, `ArchiveAsync`, `ExportScoresheet` / `ImportScoresheetAsync`.
- **`IQuestionAppService`** — `GetAsync`, `UpdateAsync`, `DeleteAsync`.
- **`ISectionAppService`** — `GetAsync`, `UpdateAsync`, `DeleteAsync`.
- **`IScoresheetInstanceAppService`** — `CreateAsync`, `GetByCorrelationAsync`, `ValidateAnswersAsync(correlationId)`.

### Controllers

Thin MVC controllers under `Unity.Flex.Application/Controllers/`, used only for file-upload/download operations that don't fit a typical DTO-in/DTO-out app service call:

- **`WorksheetController`** — `/api/app/worksheet`: `GET export/{worksheetId}`, `POST import` (JSON file upload).
- **`ScoresheetController`** — `/api/app/scoresheet`: same pattern.
- **`FlexController`** / **`FlexAppService`** — shared abstract base classes wiring the `FlexResource` localization resource; not endpoints themselves.

## Command/handler pattern

Flex is driven by **ABP local events** (`ILocalEventHandler<TEto>`, `ITransientDependency`), not direct app-service-to-app-service calls from the host module — this preserves the "don't call another module's app service directly" convention used across this codebase. The host module publishes an ETO via `ILocalEventBus`; Flex's own handler performs the actual write. This is in-process and synchronous-within-request (a **local** event, not distributed) — used purely as a decoupling seam between `Unity.GrantManager` and `Unity.Flex`.

Handlers live in `Unity.Flex.Application/Handlers/`:

| Handler | Triggered by (ETO) | Does | Published from (host side) |
|---|---|---|---|
| `CreateScoresheetInstanceHandler` | `CreateScoresheetInstanceEto` | Calls `IScoresheetInstanceAppService.CreateAsync` | `GrantApplicationAppService`, when an application's form has a `ScoresheetId` and `Unity.Flex` is enabled — at creation and at resubmission/status-change |
| `CreateWorksheetInstanceByFieldValuesHandler` | `CreateWorksheetInstanceByFieldValuesEto` | Calls `WorksheetsManager.CreateWorksheetDataByFields`; if the worksheet requires collection (`worksheet.RequiresCollection()`), also calls `worksheetInstance.CollectAsync(...)` | `CustomFieldsIntakeSubmissionMapper`, on CHEFS intake form submission |
| `PersistWorksheetInstanceValuesHandler` | `PersistWorksheetIntanceValuesEto` | Delegates to `WorksheetsManager.PersistWorksheetData` | `GrantApplicationAppService`, when custom field values are saved from the UI |
| `PersistScoresheetInstanceHandler` | `PersistScoresheetInstanceEto` | Loads the `ScoresheetInstance` via repository, finds/creates the matching `Answer`, sets its value via `ValueConverter.Convert`, saves. *(Code comment flags this as tech debt — should go through the app service, not the repository, directly.)* | — |
| `PersistScoresheetSectionInstanceHandler` | `PersistScoresheetSectionInstanceEto` | Delegates to `ScoresheetsManager.PersistScoresheetData` | `AssessmentScoresheetService`, when an assessor saves a scoresheet section, and when AI-generated scoresheet answers are copied into an assessment |

`CreateWorksheetInstanceByFieldValuesHandler` explicitly documents tenant-context handling: it falls back to `eventData.TenantId` when `ICurrentTenant.Id` is null, needed for background-job contexts where there is no ambient HTTP tenant.

Almost every publish site on the host side is gated by `IFeatureChecker.IsEnabledAsync("Unity.Flex")` first.

## Reporting integration

Two parallel pipelines: one for **field metadata**, one for **instance data**.

### Field generators (`Reporting/FieldGenerators/`)

`IReportingFieldsGenerator` / `ReportingFieldsGenerator` / `ReportingFieldsGeneratorFactory`, with per-type generators:

- `CustomFieldGenerators/` — `CheckboxGroupReportingFieldsGenerator`, `DataGridReportingFieldsGenerator`, `DefaultReportingFieldsGenerator` (worksheet custom fields).
- `QuestionFieldGenerators/` — `QuestionsReportingGenerator`, `DefaultFieldsGenerator` (scoresheet questions).

`WorksheetReportingFieldsGeneratorService` / `ScoresheetReportingFieldsGeneratorService` compute a set of flattened "report keys" for a worksheet/scoresheet definition (one key per section/field/checkbox-option), stored on the entity itself (`Worksheet.ReportKeys` / `Scoresheet.ReportKeys`, plus `ReportViewName`).

### Data generators (`Reporting/DataGenerators/`)

`IReportingDataGeneratorService<TDef, TInstance>` / `ReportingDataGeneratorServiceBase`:

- **`ScoresheetsReportingDataGeneratorService.GenerateAndSet(Scoresheet, ScoresheetInstance)`** builds a `Dictionary<string, object?>` keyed by each report key, matches each key to an `Answer` (by `Question.Name == key`), converts the value via `ScoresheetsReportingDataGeneratorFactory.Create(answer).Generate()`, computes `TotalScore` (summing `Number`, `YesNo`, and `SelectList` question types via `CalculateNumberFieldScore` / `CalculateYesNoFieldScore` / `CalculateSelectListFieldScore`), and serializes the result onto `instanceValue.SetReportingData(json)`. Wrapped in a blanket try/catch — a generation failure just logs and skips; report data can be regenerated later and never blocks intake/assessment.
- **`WorksheetsReportingDataGeneratorService` / `Factory`** follow the equivalent pattern for `CustomFieldValue`s, with `CheckboxGroupReportDataGenerator`, `DataGridReportDataGenerator`, `DefaultReportDataGenerator`.

### Dynamic DB views

`WorksheetsDynamicViewGeneratorHandler` (on `WorksheetsDynamicViewGeneratorEto`) and `ScoresheetsDynamicViewGeneratorHandler` (analogous) call a Postgres stored procedure directly via raw SQL — `CALL "Reporting".generate_worksheets_view(@worksheetId)` — to materialize a queryable SQL view per published worksheet/scoresheet, scoped by `ICurrentTenant.Change(tenantId)` inside a non-transactional unit of work. This is how Flex's dynamic/JSON-shaped data becomes something `Unity.Reporting` (and Power BI-style consumers) can query relationally. See `documentation/reporting/reporting-architecture.md` for the layer this feeds into.

> **Planned direction:** this auto-generation mechanism (one view per worksheet/scoresheet, materialized automatically via `generate_worksheets_view()`) is scheduled to be removed. The stated future direction is for all reporting views to be handled explicitly through reporting configuration instead — i.e. moving from "a view is auto-generated for every published worksheet/scoresheet" to "a view exists where someone has explicitly configured one." This is a stated project direction, not yet implemented — worth confirming current status before relying on it either way.

### Reporting sync app services (maintenance/ops tooling)

`IWorksheetReportingFieldsSyncAppService` (`SyncFields`, `SyncData`) and `IScoresheetReportingFieldsSyncAppService` (`SyncQuestions`, `SyncAnswers`) are both `[Authorize(IdentityConsts.ITAdminPolicyName)]`. They iterate all tenants (or one, via an optional `tenantId`) and backfill missing `ReportKeys` / reporting data for published worksheets/scoresheets lacking a `ReportViewName`, gated on the `Unity.Reporting` feature being enabled per tenant. These are ops/support tooling for fixing drift — not part of the live request path.

## Import / export

`WorksheetImportDto` / `ExportWorksheetDto` and `ScoresheetImportDto` / `ExportScoresheetDto` (Contracts layer) define JSON import/export of an entire worksheet or scoresheet (template only — sections, fields/questions, and their definitions). The domain-layer `PrivateSetterContractResolver` / `WorksheetContractResolver` / `ScoresheetContractResolver` (Newtonsoft `IContractResolver`s) make this possible despite nearly every entity property being `private set`. Exposed over HTTP via `WorksheetController`/`ScoresheetController`'s `export`/`import` endpoints, and in the admin builder UI (Clone/Publish modals area) — see [flex-web-ui.md](flex-web-ui.md).
