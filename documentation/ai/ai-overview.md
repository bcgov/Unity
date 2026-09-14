# AI Overview

## What problem it solves

Grant assessment is reading. A single application arrives with a form submission, a scoresheet to fill in, and often a dozen PDFs and spreadsheets attached to it. Staff read all of it, summarise it, and score it — and different assessors reading the same package produce different first drafts at different speeds.

Unity.AI produces those first drafts. It does four things for an *application*:

- **summarise each attachment** so an assessor can triage a package without opening every file,
- **analyse the submission** into structured findings,
- **score the application** against the form's configured Flex scoresheet, producing an AI assessment alongside the human ones,

and two things for a *form* — the configuration artefacts that programs otherwise hand-build:

- **map** CHEFS form fields onto Unity fields,
- **generate a draft worksheet or scoresheet** from a form schema.

The line the module holds throughout: **AI output is a draft**. Application scoring lands as a separate AI assessment, never as a human's score. Form-generation output lands in a `GenerationReview` that someone must accept or discard. Attachment summaries and analysis are displayed as AI-generated content behind a legal disclaimer the user has to acknowledge.

## The four big architectural facts

### 1. Everything is a queued job behind three gates

No AI call happens on a request thread. `AIGenerationAppService.SubmitAsync` is the single funnel every route converges on, and it checks the same three things every time before anything is queued:

```text
feature enabled?        Unity.AI.Scoring, Unity.AI.AttachmentSummaries, …  (per tenant)
        ↓
permission granted?     AI.GenerateScoring, AI.GenerateAttachmentSummaries, …
        ↓
form version supplied?  required for the three form-generation operations
        ↓
queue → distributed lock → prerequisite validation → rate limit → background job
```

The pipeline is documented in [ai-generation-pipeline.md](ai-generation-pipeline.md).

### 2. Model, operation, and prompt are database rows, not code

What model to call, how many completion tokens it may spend, whether it runs items sequentially or in parallel, and the exact system and user prompt text are all rows in the `AI` schema:

```text
AIOperation ──▶ AIModel ──▶ (Azure endpoint + key from configuration)
   │  name, ExecutionMode,      name (= deployment name), Provider,
   │  CompletionTokens,         SettingsJson { MaxOutputTokenCountSupported, Temperature }
   │  IsActive
   ▼
AIPrompt (by name + version)
   SystemPrompt, UserPrompt, MetadataJson, IsActive, TenantId
```

Only the **endpoint and API key** come from configuration (`Azure:OpenAI:Endpoint`, `Azure:OpenAI:ApiKey`). Everything else is data, resolved per operation at call time and cached for five minutes. See [ai-provider-runtime.md](ai-provider-runtime.md).

### 3. Every response is validated and retried against its expected shape

The model is asked for JSON, and JSON from a language model is not trustworthy. `OpenAIRuntimeService.GenerateWithRetryAsync` wraps every call in up to **three attempts**, and an attempt that returns HTTP 200 still fails if the payload does not validate:

```text
attempt → transport → outcome ∈ { Success, TransientFailure, PermanentFailure, InvalidOutput }
                          │
              Success ────┴──▶ AIProviderPayloadValidator.Validate<Operation>(content)
                                    valid   → return
                                    invalid → downgrade to InvalidOutput, retry
              PermanentFailure ──▶ stop immediately, do not retry
```

A permanent failure (400, 401, 404) stops at once; transient failures (408, 429, 5xx) and invalid output retry until the attempts run out.

### 4. The module calls the model; the host decides what to do with the answer

`Unity.AI.Application` knows how to render a prompt and parse a response. It does not know that grant applications exist. The host's `GrantApplications/Automation/` tree supplies the input (`IAIApplicationInputDataProvider`), owns the queue and the job lifecycle, and persists results. Six `IAIGenerationOperationExecutor` implementations bridge the two.

The dependency does run both ways in one respect: the module's `AIApplicationModule` depends on `FlexApplicationModule` for scoresheet and worksheet types.

## Module layout and dependency direction

```text
Unity.AI.Domain.Shared          → AIFeatures, AISettings, PromptType,
   (Unity.AI.Shared.csproj)       AIResource + AILocalizationKeys
        ↑
Unity.AI.Application.Contracts  → DTOs, I*AppService, AIGenerationOperations definitions,
                                  Requests/, Responses/, Models/, AIPermissions,
                                  IAIService, ITextExtractionService, IAIRateLimiter
        ↑
Unity.AI.Application            → Domain/            AIModel, AIOperation, AIPrompt
                                  EntityFrameworkCore/ ConfigureAI()
                                  Runtime/Execution/  Azure OpenAI client + transport,
                                                      config/prompt resolution, validators, parsers
                                  Operations/         analysis, scoring, attachment summary,
                                                      execution mode + strategy
                                  Generation/, RateLimit/, Prompts/, Settings/,
                                  Extraction/, DataSeed/
        ↑
Unity.AI.Web                    → Prompts pages, AIReporting page, AI settings group,
                                  AIConfiguration widget, legal disclaimer modal
```

The host wires the module in at four points:

- `GrantManagerApplicationModule` → `[DependsOn(typeof(AIApplicationModule))]`
- `GrantManagerWebModule` → `[DependsOn(typeof(AIWebModule))]`
- `GrantManagerHttpApiClientModule` → the AI contracts assembly
- `GrantManagerDbMigratorModule` → `typeof(AIApplicationModule)`, with the comment *"Needed to seed AI prompt data"*
- `GrantManagerDbContext.OnModelCreating` → `modelBuilder.ConfigureAI()`

Note the last one: `ConfigureAI()` is called on the **host** context, not the tenant context. See [ai-domain-model.md](ai-domain-model.md#two-ai-schemas-in-two-databases).

## The gating model

AI is the most heavily gated area of the product, and the gates are not interchangeable — a feature is what the tenant *has bought*, a setting is what the tenant has *turned on*, a permission is what *this user* may do, and a form flag is what *this program* wants.

### Features — seven, all default `false`

Defined in the host's `GrantManagerFeaturesDefinitionProvider`, named in `AIFeatures`:

| Feature | Gates |
|---|---|
| `Unity.AI.AttachmentSummaries` | Attachment summary generation and its view/generate permissions |
| `Unity.AI.ApplicationAnalysis` | Application analysis |
| `Unity.AI.Scoring` | Application scoring, and the AI-assessment handler |
| `Unity.AI.FormMapping` | Form mapping |
| `Unity.AI.FormWorksheet` | Form worksheet generation |
| `Unity.AI.FormScoresheet` | Form scoresheet generation |
| `Unity.AIReporting` | The AI Reporting page and its permissions |

Every `AIPermissions` entry carries `.RequireFeatures(...)` for its matching feature, so disabling a feature also removes its permissions from the UI. The `SettingManagement.ConfigureAI` permission uses an `AnyFeaturePermissionStateProvider` — it appears when *any* of the seven is on.

### Tenant settings — three, all default `"false"`

Defined in `AISettingDefinitionProvider`, tenant-scoped (`TenantSettingValueProvider`), not visible to clients, not inherited:

| Setting | Meaning |
|---|---|
| `GrantManager.AI.AutomaticGenerationEnabled` | The intake pipeline may fire automatically |
| `GrantManager.AI.ManualGenerationEnabled` | Staff may trigger generation by hand |
| `GrantManager.AI.ReportingEnabled` | The AI Reporting page is reachable |

Read and written through `AIConfigurationAppService` (`api/app/ai/configuration/tenant`), both ends requiring `SettingManagement.ConfigureAI`.

### Per-form flags

`ApplicationForm.AutomaticallyGenerateAIAnalysis` and `ManuallyInitiateAIAnalysis` — surfaced by the `AIConfiguration` view component, which only shows each toggle when the corresponding tenant setting is on. Automatic intake generation requires the tenant setting **and** the form flag.

### Permissions

Group `AI`, from `AIPermissionDefinitionProvider` — a `View*` permission per operation with a `Generate*` child under it, plus `AI.Reporting` → `AI.Reporting.CreateEditDataModel`, plus `SettingManagement.ConfigureAI` grafted onto ABP's settings group. Prompt administration is gated separately by the **`ITOperations` role**, not by an AI permission.

## External dependencies

| System | Used for | Reached through |
|---|---|---|
| **Azure OpenAI** | Every model call | `OpenAIChatClientFactory` → `AzureOpenAIClient(endpoint, ApiKeyCredential).GetChatClient(deploymentName)`; endpoint and key from `Azure:OpenAI:Endpoint` / `Azure:OpenAI:ApiKey` |
| **Distributed cache** (Redis) | The per-user generation cooldown | `AIRateLimiter`, key `ai-generation:cooldown:{userId}` |
| **Distributed locks** (Medallion) | Dedupe of queued requests, and cooldown read-modify-write | `IDistributedLockProvider`, keys `ai-generation:{tenant}:{app}:{operation}` and `ai-generation:cooldown-lock:{userId}` |
| **ABP background jobs** | Running every operation off the request thread | `IBackgroundJobManager.EnqueueAsync(AIGenerationBackgroundJobArgs)` |
| **In-process memory cache** | Operation settings and operation snapshots, 5 minutes | `IMemoryCache`, added by `AIApplicationModule` |
| **CHEFS** | Attachment content for summarisation | Host's `ChefsFileAttachmentStreamProvider` implementing `IAttachmentContentProvider` |
| **Metabase / AI reporting host** | The embedded AI Reporting page | `DynamicUrl` row `REPORTING_AI` via `IEndpointManagementAppService` |

Text extraction is **local**, not a service: PDFs via `UglyToad.PdfPig`, Office formats via `NPOI` and raw OpenXML.

## Core concepts (glossary)

| Term | Meaning |
|---|---|
| **Operation type** | The kebab-case public identifier used on the API and in job args: `attachment-summary`, `application-analysis`, `application-scoring`, `form-mapping`, `form-worksheet`, `form-scoresheet`. |
| **Operation name** | The PascalCase identifier used for the `AIOperation` row and the prompt family: `AttachmentSummary`, `ApplicationAnalysis`, `ApplicationScoring`, `FormMapping`, `FormWorksheet`, `FormScoresheet`. Bridged by `AIGenerationOperationDefinition`. |
| **`AIGenerationOperationDefinition`** | The static registry entry tying an operation type to its feature, disabled-message key, generate and view permissions, and whether it needs a form version. |
| **`AIModel`** | A row naming a deployment (`gpt-4o-mini`, `gpt-5-mini`, `gpt-5-nano`), its provider, and `SettingsJson` holding `MaxOutputTokenCountSupported` and `Temperature`. |
| **`AIOperation`** | A row binding an operation name to a model, an `ExecutionMode`, and a `CompletionTokens` budget. |
| **`AIPrompt`** | A versioned system+user prompt pair for one prompt family, optionally tenant-specific, with `MetadataJson` holding reusable template sections. |
| **Prompt family / version** | `AIPrompt.Name` is the family (equal to the operation name); `VersionNumber` selects the variant. `(TenantId, Name, VersionNumber)` is unique. Resolution prefers the current tenant's row, then the global (`TenantId == null`) row. |
| **`AIGenerationRequest`** | The queued-request row: application, operation, `Queued`/`Running`/`Completed`/`Failed`, timestamps, failure reason. Stored in `AI.AIRequests` in the **host** database. |
| **`GenerationReview`** | Draft output from a form-generation operation awaiting human acceptance — `Active`, `Completed` or `Discarded`, keyed `(Operation, ContextId, Sequence)`. |
| **`AIExecutionMode`** | `Sequential` (default), `Parallel`, or `Batch` — how a multi-item operation (many attachments, many scoresheet sections) fans out. Persisted per operation. |
| **`AIOperationOutcome`** | `Success`, `TransientFailure`, `PermanentFailure`, `InvalidOutput` — the retry loop's control signal. |
| **`AIFailureCategory`** | `None`, `ProviderUnavailable`, `TransientProviderFailure`, `PermanentProviderFailure`, `InvalidOutput` — the reason recorded for logging. |
| **Cooldown** | A per-user rate limit stamped **after** a successful generation, of `Azure:Generation:CooldownSeconds` seconds. System and background callers (no current user) bypass it. |
| **Placeholder** | A `{{TOKEN}}` slot in a prompt's user template — `{{DATA}}`, `{{SCHEMA}}`, `{{ATTACHMENTS}}`, `{{SECTION}}`, `{{RESPONSE}}`/`{{OUTPUT}}`, plus any section defined in the prompt's `MetadataJson`. An unresolved placeholder throws. |

## Read in this order

1. **[ai-overview.md](ai-overview.md)** (this file).
2. **[ai-domain-model.md](ai-domain-model.md)** — entities, the two schemas, seeders, permissions, settings.
3. **[ai-generation-pipeline.md](ai-generation-pipeline.md)** — queue to result.
4. **[ai-provider-runtime.md](ai-provider-runtime.md)** — the Azure OpenAI call itself.
5. **[ai-prompts.md](ai-prompts.md)** — prompt families, versions and rendering.
6. **[ai-operations.md](ai-operations.md)** — the six operations in detail.
7. **[ai-web-ui.md](ai-web-ui.md)** — pages, widgets, gating.
8. **[ai-roadmap.md](ai-roadmap.md)** — known rough edges.
