# AI Domain Model

The module's own entities live in `modules/Unity.AI/src/Unity.AI.Application/Domain/` — there is no separate `.Domain` project. The entities that track *work* rather than *configuration* belong to the host.

## Configuration entities (module-owned)

### `AIModel` — `Domain/AIModel.cs`

`AuditedAggregateRoot<Guid>`. **Not** `IMultiTenant` — models are global.

| Property | Notes |
|---|---|
| `Name` | Unique. Doubles as the **Azure deployment name** — `OpenAIChatClientFactory` passes it straight to `GetChatClient(...)` |
| `Provider` | Only `"OpenAI"` is accepted anywhere; see [ai-roadmap.md](ai-roadmap.md#the-provider-is-hard-coded-in-three-places) |
| `Type` | `Chat` or `Embedding` (`AIModelType`, stored as a string). AI operations and the AI Reporting chat model need a `Chat` model; the AI Reporting embedding model needs an `Embedding` one. Can't change while the model is in use |
| `ApiVersion` | The Azure OpenAI `?api-version=` sent with every call to this deployment, by UGM and by AI Reporting (for example `2024-10-21`, `2025-04-01-preview`). Required, checked only for form (`AzureOpenAIApiVersions.IsValid`); whether Azure accepts it shows on Test. Migration `AddAIModelApiVersion` set existing rows to `2024-10-21` |
| `IsActive` | An inactive model makes every operation bound to it throw |

There are no per-model request settings. With `gpt-4o-mini` retired (AB#35030), every model in use is a gpt-5 reasoning model, so the transport sends neither a temperature nor an output token cap (the `SettingsJson` column was dropped in migration `RemoveAIModelSettingsAndCompletionTokens`, AB#34947).

### `AIOperation` — `Domain/AIOperation.cs`

`AuditedAggregateRoot<Guid>`. Not `IMultiTenant`.

| Property | Notes |
|---|---|
| `Name` | Unique. The **operation name** (`ApplicationScoring`, …), which is also the prompt family name |
| `AIModelId` / `AIModel` | Required FK, `DeleteBehavior.Restrict` |
| `ExecutionMode` | `Sequential` \| `Parallel` \| `Batch`, stored as a string, max length 20 |
| `IsActive` | Inactive operations are invisible to the resolver and cannot be queued |

### `AIPrompt` — `Domain/AIPrompt.cs`

`AuditedAggregateRoot<Guid>`, **`IMultiTenant`** — with `TenantId` having a `protected` setter.

| Property | Notes |
|---|---|
| `Name` | The prompt family, matching an `AIOperation.Name` |
| `VersionNumber` | Selects the variant within the family |
| `SystemPrompt` / `UserPrompt` | `text`, both required |
| `MetadataJson` | `jsonb`, default `"{}"` — named sections usable as extra placeholders |
| `IsActive` | An inactive prompt makes the operation throw at resolution time |

Unique index on `(TenantId, Name, VersionNumber)`. A tenant may therefore hold its own version 2 of `ApplicationScoring` alongside the global version 2, and resolution prefers the tenant's. See [ai-prompts.md](ai-prompts.md#how-a-prompt-is-chosen).

`PromptType` (`Orchestrator`, `Skill`, `Instruction`, `Agent`) exists in `Unity.AI.Domain.Shared` but is not a property of `AIPrompt` and is not referenced by the runtime.

## Work entities (host-owned)

### `AIGenerationRequest` — `src/Unity.GrantManager.Domain/GrantApplications/AIGenerationRequest.cs`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. One row per queued generation.

`ApplicationId`, `OperationId`, `Status` (`AIGenerationRequestStatus`: `Queued`=0, `Running`=1, `Completed`=2, `Failed`=3), `StartedAt`, `CompletedAt`, `FailureReason` (max 2000). `IsActive` is computed as `Queued or Running` and is what both the dedupe check and the rate limiter's "is this user already generating?" test read.

Transitions are three methods — `MarkRunning`, `MarkCompleted`, `MarkFailed` — each stamping a timestamp. Note that `MarkRunning` and `MarkCompleted` both **clear** `FailureReason`, so a retried request loses the record of why it failed last time.

Mapped in `GrantManagerDbContext` to `AI.AIRequests`, with indexes on `OperationId` and on `(TenantId, ApplicationId, OperationId, Status)` — the exact shape the dedupe query needs.

### `ApplicationScoresheetAnswers` — `src/Unity.GrantManager.Domain/Applications/`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. One row per application, holding the whole AI answer set as `jsonb` keyed by scoresheet question id. Unique index on `ApplicationId`.

Its own doc comment records why it exists: the answers used to be an `Applications.AIScoresheetAnswers` column, and were moved to their own table in the `AI` schema *"so the Applications table stays legible to report builders, and so AI output is not written back onto the Application aggregate."*

### `GenerationReview` — `src/Unity.GrantManager.Domain/ApplicationForms/`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. The human-in-the-loop record for form-generation operations.

`Operation` (the operation type string), `ContextId` (the form version id), `Sequence`, `Status` (`GenerationReviewStatus`: `Active`, `Completed`, `Discarded`), and `ReviewData` as `jsonb`. Unique index on `(Operation, ContextId, Sequence)`.

An `Active` review blocks a new generation for the same operation and form version — `AIGenerationPrerequisiteValidator.EnsureNoActiveReviewAsync` throws `AI:FormGenerationReviewActive`.

## Two `AI` schemas in two databases

This is the single most surprising fact about AI persistence: there is an `AI` schema in **both** databases, and they hold different things.

| Database | Context | Schema `AI` contains |
|---|---|---|
| **Host** | `GrantManagerDbContext` — calls `modelBuilder.ConfigureAI()` | `AIModels`, `AIOperations`, `AIPrompts`, `AIRequests` |
| **Tenant** | `GrantTenantDbContext` — explicit `ToTable(..., "AI")` mappings | `GenerationReviews`, `ApplicationScoresheetAnswers` |

`AIDbProperties`: `DbSchema = "AI"`, `DbTablePrefix = ""`.

Consequences worth knowing:

- **`AIPrompt` is `IMultiTenant` but lives in the host database.** Every read path therefore wraps itself in `IDataFilter<IMultiTenant>.Disable()` and re-applies tenancy by hand — `AIPromptTemplateStore`, `OpenAIConfigurationResolver.ResolvePromptAsync`, and every method of `AIPromptAppService`. ABP's automatic tenant filter is *not* what isolates prompts.
- **`AIRequests` references applications that live in a different database.** No foreign key is possible, and none is declared.
- Schema changes split by target: model/operation/prompt/request changes are **host** migrations; review and scoresheet-answer changes are **tenant** migrations.

```bash
cd applications/Unity.GrantManager/src/Unity.GrantManager.EntityFrameworkCore

# AIModels / AIOperations / AIPrompts / AIRequests
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# GenerationReviews / ApplicationScoresheetAnswers
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```

## Seed data

`AIDataSeeder` (`IDataSeedContributor`) runs one seeder: prompts. It **skips tenant contexts** (`if (context.TenantId != null) return;`), so this is host-level seed data. `GrantManagerDbMigratorModule` depends on `AIApplicationModule` specifically to make this run.

**AI models and AI operations are not seeded** (AB#34947). They are environment configuration: which models exist depends on the deployments in each environment's Azure OpenAI account. They are managed in Unity Admin → **AI Setup** (see [Unity AI Setup](#unity-ai-setup) below): IT Administrator adds, edits and deletes models; IT Operations assigns models to operations, and the DbMigrator never creates or changes them. Existing rows stay as they are across deploys. A new or empty database starts with no models and no operations, so every AI operation fails with `AI operation '...' is not configured.` until AI Setup is done. AI generation is off by default (features and settings default to false), so nothing else breaks.

The values used in every environment when seeding stopped, as a starting point for a new database:

| Model (Azure deployment name) | Provider | Type | API version |
|---|---|---|---|
| `gpt-5-mini` | OpenAI | Chat | `2024-10-21` |
| `gpt-5-nano` | OpenAI | Chat | `2024-10-21` |
| `text-embedding-3-large` | OpenAI | Embedding | `2024-10-21` |

AI Reporting (AB#35030): chat `gpt-5-mini`, embedding `text-embedding-3-large`.

| Operation | Model | `ExecutionMode` |
|---|---|---|
| `ApplicationScoring` | `gpt-5-mini` | `Sequential` |
| `FormScoresheet` | `gpt-5-mini` | `Sequential` |
| `ApplicationAnalysis` | `gpt-5-mini` | `Sequential` |
| `FormWorksheet` | `gpt-5-mini` | `Sequential` |
| `AttachmentSummary` | `gpt-5-mini` | `Sequential` |
| `FormMapping` | `gpt-5-mini` | `Sequential` |

`gpt-4o-mini` is **retired** (AB#35030). Existing rows were left inactive by the old model seeder; delete them in AI Setup once no operation points at them. An operation pointing at an inactive model fails with `AI model 'gpt-4o-mini' is inactive.` instead of calling a deployment that may not exist.

### Prompts — `AIPromptDataSeeder`

**Insert-only**: it inserts a `Name` + `VersionNumber` only when that name has no version at or above it (so an older version deleted in AI Prompts is not recreated), and never changes an existing one, so an edit or deactivation made in AI Prompts survives the next deploy. New prompt text in code needs a **new `VersionNumber`**; changing the text of an existing version in code has no effect on databases that already have it.

973 lines, almost all of it prompt text held in C# `const string` fields. Seeds global (`TenantId == null`) prompts:

| Family | Versions seeded |
|---|---|
| `ApplicationAnalysis` | v0, v1, v2 |
| `AttachmentSummary` | v0, v1, v2 |
| `ApplicationScoring` | v0, v1, v2 |
| `FormMapping` | v2 |
| `FormWorksheet` | v2 |
| `FormScoresheet` | v2 |

The three form-generation families also seed `MetadataJson`. See [ai-prompts.md](ai-prompts.md).

## Permissions

Group `AI`, declared in `AIPermissionDefinitionProvider` (in **Application.Contracts**, unlike most modules which put the provider in the Application project). Every entry carries `.RequireFeatures(...)`.

| Permission | Child | Feature required |
|---|---|---|
| `AI.Reporting` | `AI.Reporting.CreateEditDataModel` | `Unity.AIReporting` |
| `AI.ViewApplicationAnalysis` | `AI.GenerateApplicationAnalysis` | `Unity.AI.ApplicationAnalysis` |
| `AI.ViewAttachmentSummary` | `AI.GenerateAttachmentSummaries` | `Unity.AI.AttachmentSummaries` |
| `AI.ViewScoringResult` | `AI.GenerateScoring` | `Unity.AI.Scoring` |
| `AI.ViewFormMapping` | `AI.GenerateFormMapping` | `Unity.AI.FormMapping` |
| `AI.ViewFormWorksheet` | `AI.GenerateFormWorksheet` | `Unity.AI.FormWorksheet` |
| `AI.ViewFormScoresheet` | `AI.GenerateFormScoresheet` | `Unity.AI.FormScoresheet` |

Plus `SettingManagement.ConfigureAI`, grafted onto ABP's `SettingManagement` group with an `AnyFeaturePermissionStateProvider` over all seven features — it is visible when any one of them is enabled.

`AIPermissions` also exposes per-operation aliases (`AIPermissions.ApplicationScoring.View`/`.Generate`, etc.) that point at the same `Analysis.*` constants; both spellings appear in the codebase.

**Prompt and AI Setup administration is not in this group.** `AIPromptAppService` is `[Authorize(IdentityConsts.ITOperationsPolicyName)]` and sets every CRUD policy to the same, so prompts are governed by the `ITOperations` role. `AIModelAppService` and `AIOperationAppService` (Unity AI Setup) are `[Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]` for reading (and model Test). Changing a model (`CreateAsync`, `UpdateAsync`, which includes active/inactive, and `DeleteAsync`) also needs `ITAdminPolicyName`, because a model mirrors an Azure deployment; changing an operation (`AIOperationAppService.UpdateAsync`) also needs `ITOperationsPolicyName`.

## Unity AI Setup

Host-wide models and operations in Unity Admin → AI Setup (next to AI Prompts). Both IT roles see the page and can Test a model; only IT Administrator adds, edits or deletes models, and only IT Operations edits operations. Rules live in `AISetupManager` (`Unity.AI.Application/Domain`):

| Service | Route | Does |
|---|---|---|
| `AIModelAppService` | `api/app/ai/setup/models` | List, get, create, update, delete. `POST {id}/test` makes one small chat call to the deployment with `Azure:OpenAI:Endpoint` / `ApiKey` (`AIModelConnectionTester`) and returns success, HTTP status, Azure error code (such as `DeploymentNotFound`) and latency |
| `AIOperationAppService` | `api/app/ai/setup/operations` | Lists the six built-in operations (`AIPromptTypes.All`), configured or not, plus any other rows, with the highest active host prompt version. `PUT {name}` sets model, execution mode and on/off, and creates the row for a built-in operation that isn't configured yet |

Rules:

- A model name (the Azure deployment name) is unique, case-insensitive (unique index `IX_AIModels_Name_Lower` on `lower("Name")`). Only provider `OpenAI` is accepted, because the runtime supports no other.
- A model's API version must look like `2024-10-21` or `2025-04-01-preview` (`AI:SetupApiVersionInvalid`). Any published version can be used, not only those the SDK knows: see [ai-provider-runtime.md](ai-provider-runtime.md#api-version).
- A model that **any** operation points at can't be deactivated or deleted.
- An operation can only point at an **active** model of the right type (`AI:SetupModelNotChat`, `AI:SetupModelNotEmbedding`). Execution mode is `Sequential`, `Parallel` or `Batch`, and only `ApplicationScoring` may use `Parallel` or `Batch` (the other operations never read their mode).
- **AI Reporting** has two operation rows, `AIReporting` (chat model, writes the SQL) and `AIReportingEmbedding` (embedding model, matches questions to tables), shown in their own AI Reporting section rather than the Model Assignments table (`AISetupConsts.AIReportingOperations`); both are always active (the server ignores an inactive value for them). `AIReportingModelResolver` reads them; `JWTTokenAppService` puts each model's deployment and API version in the AI Reporting token (`ai_chat_deployment`, `ai_chat_api_version`, `ai_embedding_deployment`, `ai_embedding_api_version`) and refuses the token (`AI:ReportingModelsNotSet`) until both are set. AI Reporting has no models of its own. Changing the embedding model makes AI Reporting re-embed each program's schema the next time it is opened.
- `GET endpoint-status` (`AIEndpointHealthChecker`) calls `Azure:OpenAI:Endpoint` + `/healthz` from the server, with no key and a five-second timeout, so it works for the in-cluster LLM proxy (`http://unity-azure-llm-proxy/`), which has no route. The page shows the proxy's answer, which names the Azure account it forwards to: **Healthy** for a 2xx, **Unhealthy** with the proxy's message for any other error (such as a 503 when Azure is down), and "no /healthz status" only for a 404 or 405.
- Every save (models, operations, and AI Prompts) calls `AIConfigurationCacheSignal.Reset()`, which expires the resolver's cached settings on that server. Other web pods pick the change up when their five-minute cache entries expire. See [ai-provider-runtime.md](ai-provider-runtime.md#caching).

## Settings

`AISettingDefinitionProvider` defines three, all defaulting to `"false"`, all `TenantSettingValueProvider`-scoped, `isVisibleToClients: false`, `isInherited: false`:

| Key | Read by |
|---|---|
| `GrantManager.AI.AutomaticGenerationEnabled` | `QueueApplicationAIPipelineOnProcessHandler`, `AIConfiguration` widget |
| `GrantManager.AI.ManualGenerationEnabled` | `AIConfiguration` widget |
| `GrantManager.AI.ReportingEnabled` | `AIMenuContributor`, AI Reporting page |

Configuration keys (not ABP settings — `IConfiguration`, so environment/secret-backed):

| Key | Used by | Behaviour if missing |
|---|---|---|
| `Azure:OpenAI:Endpoint` | `OpenAIConfigurationResolver` | `InvalidOperationException`; must be an absolute URI |
| `Azure:OpenAI:ApiKey` | `OpenAIConfigurationResolver` | `InvalidOperationException` |
| `Azure:Generation:CooldownSeconds` | `AIRateLimiter` | `AbpException` — must be a positive value |
| `Azure:Logging:EnablePromptFileLog` | `OpenAIPromptFileLogger` | Defaults to `false` |

## Localization and error keys

`AIErrorCodes` (`Unity.AI.Domain.Shared/AIErrorCodes.cs`) holds every `AI:` error code the module and the host app throw as a `BusinessException`. They fall into four groups: per-operation *disabled* codes used by `AIFeatureGuard`, per-operation *prerequisite* codes used by `AIGenerationPrerequisiteValidator`, form-generation review codes, and scoresheet-generation validation codes. Each code is also its key in `AIResource`, which `AISharedModule` maps to the `AI` code namespace so ABP shows the localized message. Codes that need values (section name, property name, failure reason) pass them with `WithData`, and the `en.json` text uses the matching named placeholders.

Non-error keys, such as the guided workflow step names (`AI:Workflow*`), are written inline as `L["..."]` / `localizer["..."]` like every other module.

`AIFeatureGuard.EnsureEnabledAsync(featureName, disabledErrorCode)` is the single place a disabled feature becomes a `BusinessException`, and the code it uses comes from the operation's `AIGenerationOperationDefinition.DisabledErrorCode`.
