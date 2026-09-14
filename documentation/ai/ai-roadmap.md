# AI Roadmap — Known Rough Edges

Things worth knowing before extending this module. This is not a backlog; it is the set of behaviours that surprise people reading the code for the first time. If you fix one, delete it from here in the same commit.

This module is newer and better covered by tests than most of the codebase — the list below is shorter on outright defects and longer on "correct but surprising" than the equivalent page for Payments.

## Failures are persisted as content

Four of the six operations turn a provider failure into a *successful* result carrying a failure message, which is then written to the database as if it were output:

| Operation | What gets stored on failure |
|---|---|
| Attachment summary | `"AI analysis not available for this attachment ({fileName})."` as the summary |
| Attachment summary (no extractable text) | `"Attachment text could not be extracted for AI summary generation."` |
| Application scoring | An empty answer set — no error anywhere |
| Form worksheet | `"{}"` plus a `FailureReason` the caller may ignore |

The generation request is marked `Completed` in all of these cases, the user's cooldown is charged, and the UI shows a summary that reads like a summary. Only **application analysis** throws, marking the request `Failed` with the provider's response in the log.

The sentinel strings are also indistinguishable from real output once stored — a later reader cannot tell "the model was down" from "the model said this".

## A partly failed scoring run is saved as if complete

`ApplicationScoringService.ProcessSectionAsync` catches any exception per section, logs it, and returns an empty dictionary:

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Error processing AI application scoring section {SectionName} for application {ApplicationId}", ...);
}
return sectionResults;   // empty
```

The merge across sections then produces an answer set silently missing those sections, `SaveScoresheetAnswersInNewUowAsync` upserts it over any previous complete run, and `ApplicationAIScoringGeneratedEvent` fires as normal — creating an AI assessment from a partial scoresheet.

## The intake pipeline is three peers, not a chain

`QueueApplicationIntakeAsync` queues attachment summary, application analysis and scoring as three independent background jobs. Nothing orders them.

But **application analysis and scoring both consume attachment summaries as input** (`GetAttachmentSummariesAsync`). At intake all three start at once, so analysis and scoring typically read whatever summaries existed *before* this run — on a first intake, none. The prompt still renders, with an empty attachments block.

## The retry loop has no backoff

`GenerateWithRetryAsync` runs up to three attempts back to back with no delay, including after a `TransientFailure` mapped from **HTTP 429**. Three immediate retries against a rate-limited endpoint are unlikely to be the intended behaviour, and there is no jitter to spread concurrent jobs.

`AIExecutionMode.Parallel` compounds this: `Task.WhenAll` over every section or attachment with **no concurrency cap**, so a scoresheet with twenty sections issues twenty simultaneous calls, each of which may retry three times.

## Configuration caching is per-pod and holds the API key

`ResolveOperationSettingsAsync` caches the fully resolved `OpenAIOperationSettings` — including `ApiKey` — in `IMemoryCache` for five minutes, keyed `ai:operation-settings:{tenantId|host}:{operation}`. A second entry caches the operation snapshot.

Consequences:

- **Changes take up to five minutes**, independently per pod, with no invalidation on write. Activating a new prompt version, switching an operation's model, or changing a token budget applies at different times on different replicas.
- The API key sits in process memory in a cache keyed by a predictable string.
- `AddMemoryCache()` is called by `AIApplicationModule`, so this is in-process by design, not a misconfigured distributed cache.

## The provider is hard-coded in three places

Despite `AIModel.Provider` being a column:

1. `OpenAIConfigurationResolver.ResolveProviderName()` returns the constant `"OpenAI"`.
2. `ResolveOperationSettingsAsync` throws `"AI provider '{x}' is not supported"` for anything else.
3. `OpenAITransportService.GenerateSummaryAsync` returns `PermanentFailure` for anything else.

The configuration keys are also provider-interpolated but effectively fixed: `Azure:OpenAI:Endpoint`, `Azure:OpenAI:ApiKey`.

Related naming confusion: the client is `AzureOpenAIClient` (Azure OpenAI), while the provider string is `"OpenAI"` and the config section is `Azure:OpenAI:` — three different framings of the same thing in one call path.

## Deployment name and model name must be identical

`OpenAIOperationSettings` sets both `ProfileName` and `DeploymentName` to `model.Name`, and the factory passes `DeploymentName` to `GetChatClient(...)`. The Azure deployment must therefore be named exactly `gpt-5-mini`, `gpt-4o-mini` or `gpt-5-nano`. There is no way to point a model row at a differently named deployment.

## Dead resolver accessors

`OpenAIConfigurationResolver` exposes seven accessors that production code never calls — the live path is `ResolveOperationSettingsAsync` alone:

| Method | Called by |
|---|---|
| `ResolveConfiguredTemperatureAsync` | tests only |
| `ResolveEndpointAsync` | tests only |
| `ResolveDeploymentNameAsync` | tests only |
| `ResolveMaxOutputTokenCountSupportedAsync` | tests only |
| `ResolveProfileNameAsync` | nothing |
| `ResolveCompletionTokensAsync` | nothing |
| `ResolvePromptVersionAsync` | nothing |

Worse, five of them are unusable without an explicit model name: `ResolveModelAsync` returns `null` when `modelName` is null or blank, so calling any of them with no argument throws `"AI model is not configured."` regardless of how the system is set up.

## `IsAvailableAsync` only checks that a key is configured

```csharp
await _openAIConfigurationResolver.ResolveApiKeyAsync();
return true;
```

`ResolveApiKeyAsync` reads `Azure:OpenAI:ApiKey` and nothing else. A wrong key, an unreachable endpoint, a missing operation row or an inactive model all still report *available*.

## Tenant isolation for prompts is manual

`AIPrompt` implements `IMultiTenant`, but the table lives in the **host** database and every read path disables the tenant filter (`IDataFilter<IMultiTenant>.Disable()`) so it can fall back to the global row. Isolation is then re-imposed by hand:

- `AIPromptAppService.EnsureReadAccess` / `EnsureMutationAccess`
- `AIPromptTemplateStore` and `OpenAIConfigurationResolver.ResolvePromptAsync` filter in LINQ

ABP's automatic filter protects nothing here. Any new query over `AIPrompt` has to repeat the pattern, and a missed check is a cross-tenant read of prompt text.

Related: `AIRequests` also lives in the host database while the applications it references live in tenant databases, so no foreign key exists or can exist.

## The prompt list pages in memory

`AIPromptAppService.GetListAsync` calls `Repository.GetListAsync()` with no predicate — every prompt row, every family, every version, every tenant — then filters, sorts and applies `Skip`/`Take` in memory. Fine at the current row count; not a paging implementation.

## A tenant prompt silently overrides the whole family

`ResolvePromptAsync` prefers the current tenant's newest active prompt and only falls back to global rows when the tenant has **none**:

```csharp
var selected = currentTenant.Id is Guid tenantId
    ? prompts.Where(p => p.TenantId == tenantId).OrderByDescending(p => p.VersionNumber).FirstOrDefault()
    : null;
selected ??= prompts.Where(p => p.TenantId == null).OrderByDescending(p => p.VersionNumber).FirstOrDefault();
```

So a tenant that once saved a tweak to `ApplicationScoring` v1 stops receiving every subsequent global improvement to that family, including a seeded v3, with nothing in the UI indicating the divergence.

## Placeholder errors surface at run time, not save time

`AIPromptTemplateRenderer` throws `"Unresolved prompt placeholders: ..."` (or `"Invalid prompt placeholders: ..."`) while rendering. Nothing validates a prompt when it is saved through the Prompts UI, so a typo in a `{{TOKEN}}` breaks that operation for that tenant at the next generation, appearing as a failed request rather than a validation error on the form.

## Prompt text lives in a 973-line C# file

`AIPromptDataSeeder` is almost entirely `const string` prompt bodies. Editing a seeded prompt means a code change, a build and a deploy; diffs are unreadable; and there is no way to see prompt history except through git. The runtime already supports versioned rows, so the seeder is the only reason prompt text is compiled in.

## Form scoresheet does not check its outcome

Every other operation branches on `result.Outcome` before using `result.Content`. `GenerateFormScoresheetAsync` does not:

```csharp
return new FormScoresheetResponse
{
    Scoresheet = AIResponseJson.CleanJsonResponse(result.Content),
    FailureReason = result.FailureReason
};
```

On a failed call this returns the cleaned content of a failure response as if it were a scoresheet, relying entirely on the caller to notice `FailureReason`. Its sibling `GenerateFormWorksheetAsync` returns `"{}"` in the same situation.

## Prompt file logging writes applicant data to local disk

`OpenAIPromptFileLogger`, when `Azure:Logging:EnablePromptFileLog` is `true`, appends the full rendered prompt — submission data, attachment text — and the raw provider response to `{AppContext.BaseDirectory}/logs/ai-prompts-*.log`. It is off by default and called on every operation. Useful for prompt engineering; worth a deliberate decision before enabling anywhere holding real applications.

## `MarkRunning` and `MarkCompleted` clear the failure reason

```csharp
public void MarkRunning(DateTime startedAt) { Status = Running; StartedAt = startedAt; FailureReason = null; }
```

A request that failed and is retried loses the record of why it failed the first time. The only trace is the log.

## The status DTO carries its payload twice

`AIGenerationAppService.GetStatusAsync` returns the same nine fields nested under `GenerationRequest` **and** flattened onto the root of `AIGenerationStatusDto`. Both are populated from the same source on every call. Presumably a compatibility shim for two client versions; one of the two shapes should win.

## Duplicate submissions return silently

`EnsureRequestAndEnqueueAsync` returns without an exception when a `Queued` or `Running` request already exists for the same tenant, application and operation. The caller receives a 200 and cannot distinguish "queued" from "already running" — the UI has to poll the status endpoint to find out.

## No admin UI for models or operations

Prompts have a full CRUD area. `AIModel` and `AIOperation` — which decide the model, token budget and execution mode for every call — have none, and their seeders are **corrective**: `AIModelDataSeeder` and `AIOperationDataSeeder` overwrite provider, active flag, settings JSON, model binding, execution mode and token budget on every run. Any change made directly in the database is reverted at the next migration run.

## Convention drift

- **`Guid.CreateVersion7()` instead of `GuidGenerator`.** `AIPromptAppService.CreateAsync`, `AIModelDataSeeder` and `AIOperationDataSeeder` all bypass ABP's `IGuidGenerator`, which the repo's `.claude/rules/csharp.md` requires. The host-side executors do use `IGuidGenerator`.
- **`DateTime.UtcNow` instead of `Clock`.** Throughout the job helpers, the rate limiter and the prompt file logger.
- **Two naming schemes for the same six things.** `AIGenerationOperations` uses kebab-case (`application-scoring`) for the API and job args; `AIPromptTypes` uses PascalCase (`ApplicationScoring`) for operation rows and prompt families. `AIGenerationOperationDefinition.OperationName` bridges them, and `AIGenerationOperationKeyHelper` re-exports the kebab-case constants under different names again.
- **`Unity.AI.Domain.Shared` builds `Unity.AI.Shared.csproj`** — the folder and assembly names disagree.
- **The permission definition provider lives in Application.Contracts**, not the Application project as in every other module.
- **`PromptType`** (`Orchestrator`/`Skill`/`Instruction`/`Agent`) is defined in Domain.Shared, is not a property of `AIPrompt`, and is referenced nowhere.

## Test coverage

Unusually good for this codebase, and worth preserving. Existing suites in `test/Unity.GrantManager.Application.Tests/`:

| Area | Tests |
|---|---|
| Config resolution | `Runtime/Execution/OpenAIConfigurationResolverTests` |
| Prompt resolution and rendering | `AIPromptTemplateProviderTests`, `AIPromptTemplateRendererTests` |
| Response validation | `AIProviderPayloadValidatorTests` |
| Outcome model | `AIOperationResultTests` |
| Queue, job, executor registry, status | `AIGenerationQueueTests`, `AIGenerationAppServiceTests`, `AIGenerationOperationExecutorRegistryTests`, `AIGenerationStatusAppServiceTests`, `LegacyAIGenerationBackgroundJobTests` |
| Rate limiting | `AI/RateLimit/AIRateLimiterTests` |
| Seeders | `AI/DataSeed/AIModelDataSeederTests`, `AIPromptDataSeederTests` |
| Input building and execution mode | `Operations/AIApplicationInputBuilderTests`, `AIExecutionModeResolverTests` |
| Prompts service | `Prompts/AIPromptAppServiceTests` |
| Form config and mapping | `ApplicationForms/AIConfigurationTests`, `Mapping/AiDraftNameTests` |
| Scoring → assessment | `CreateAIAssessmentOnScoringGeneratedHandlerTests` |

Thinner: the transport itself (temperature fallback, response extraction, status→outcome mapping), text extraction across formats, and the six operation executors end to end.
