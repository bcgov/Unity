# AI Domain Model

The module's own entities live in `modules/Unity.AI/src/Unity.AI.Application/Domain/` — there is no separate `.Domain` project. The entities that track *work* rather than *configuration* belong to the host.

## Configuration entities (module-owned)

### `AIModel` — `Domain/AIModel.cs`

`AuditedAggregateRoot<Guid>`. **Not** `IMultiTenant` — models are global.

| Property | Notes |
|---|---|
| `Name` | Unique. Doubles as the **Azure deployment name** — `OpenAIChatClientFactory` passes it straight to `GetChatClient(...)` |
| `Provider` | Only `"OpenAI"` is accepted anywhere; see [ai-roadmap.md](ai-roadmap.md#the-provider-is-hard-coded-in-three-places) |
| `IsActive` | An inactive model makes every operation bound to it throw |
| `SettingsJson` | `jsonb`, deserialised into `AIModelSettings` |

`AIModelSettings` holds two fields that exist because model families differ in what parameters they accept:

- `MaxOutputTokenCountSupported` (default `true`) — when false, `ChatCompletionOptions.MaxOutputTokenCount` is not set at all.
- `Temperature` (nullable) — when null, temperature is not sent.

### `AIOperation` — `Domain/AIOperation.cs`

`AuditedAggregateRoot<Guid>`. Not `IMultiTenant`.

| Property | Notes |
|---|---|
| `Name` | Unique. The **operation name** (`ApplicationScoring`, …), which is also the prompt family name |
| `AIModelId` / `AIModel` | Required FK, `DeleteBehavior.Restrict` |
| `ExecutionMode` | `Sequential` \| `Parallel` \| `Batch`, stored as a string, max length 20 |
| `CompletionTokens` | The per-call output budget; must be positive or resolution throws |
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

`AIDataSeeder` (`IDataSeedContributor`) runs three seeders in order — prompts, models, operations. All three **skip tenant contexts** (`if (context.TenantId != null) return;`), so this is host-level seed data. `GrantManagerDbMigratorModule` depends on `AIApplicationModule` specifically to make this run.

### Models — `AIModelDataSeeder`

| Name | Provider | `MaxOutputTokenCountSupported` | `Temperature` |
|---|---|---|---|
| `gpt-4o-mini` | OpenAI | true | 0.3 |
| `gpt-5-mini` | OpenAI | false | — |
| `gpt-5-nano` | OpenAI | false | — |

The seeder is idempotent and **corrective**: an existing row has its provider, `IsActive` and `SettingsJson` overwritten on every run.

### Operations — `AIOperationDataSeeder`

All six bind to `gpt-5-mini` and default to `Sequential`. The differences are the token budgets, which encode how much output each operation is expected to produce:

| Operation | `CompletionTokens` |
|---|---|
| `ApplicationScoring` | 8000 |
| `FormScoresheet` | 8000 |
| `ApplicationAnalysis` | 4000 |
| `FormWorksheet` | 4000 |
| `AttachmentSummary` | 2000 |
| `FormMapping` | 2000 |

Also corrective — an existing operation has its model, execution mode, token budget and `IsActive` reset on every run. If the seeded model is missing, seeding is skipped with a warning rather than failing.

### Prompts — `AIPromptDataSeeder`

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

**Prompt administration is not in this group.** `AIPromptAppService` is `[Authorize(IdentityConsts.ITOperationsPolicyName)]` and sets every CRUD policy to the same, so prompts are governed by the `ITOperations` role.

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

`AILocalizationKeys` (`Unity.AI.Domain.Shared/Localization/`) holds ~50 keys against `AIResource`, falling into four groups: per-operation *disabled* messages used by `AIFeatureGuard`, per-operation *prerequisite* messages used by `AIGenerationPrerequisiteValidator`, form-generation review and workflow messages, and scoresheet-generation validation messages.

`AIFeatureGuard.EnsureEnabledAsync(featureName, disabledMessageKey)` is the single place a disabled feature becomes a `UserFriendlyException`, and the key it uses comes from the operation's `AIGenerationOperationDefinition`.
