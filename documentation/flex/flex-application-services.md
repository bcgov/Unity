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

Flex does not create reporting views itself. It supplies field metadata to Reporting Configuration through `Reporting/Configuration/`: `WorksheetsMetadataService` and `ScoresheetsMetadataService` (behind `IWorksheetsMetadataService` / `IScoresheetsMetadataService` in Contracts), using `WorksheetFieldSchemaParser` and `ScoresheetFieldSchemaParser` to flatten a worksheet's custom fields or a scoresheet's questions into reportable components. `Unity.Reporting`'s `WorksheetFieldsProvider`, `ConsolidatedWorksheetFieldsProvider`, and `ScoresheetFieldsProvider` call them; the views themselves read `WorksheetInstance.CurrentValue` and `Flex.Answers` directly. See [`documentation/reporting/reporting-configuration.md`](../reporting/reporting-configuration.md).

## Import / export

`WorksheetImportDto` / `ExportWorksheetDto` and `ScoresheetImportDto` / `ExportScoresheetDto` (Contracts layer) define JSON import/export of an entire worksheet or scoresheet (template only — sections, fields/questions, and their definitions). The domain-layer `PrivateSetterContractResolver` / `WorksheetContractResolver` / `ScoresheetContractResolver` (Newtonsoft `IContractResolver`s) make this possible despite nearly every entity property being `private set`. Exposed over HTTP via `WorksheetController`/`ScoresheetController`'s `export`/`import` endpoints, and in the admin builder UI (Clone/Publish modals area) — see [flex-web-ui.md](flex-web-ui.md).
