# AI Generation Pipeline

Every one of the six operations travels the same path from request to result. Nothing calls the model on a request thread.

```text
1  AIGenerationAppService.SubmitAsync(operationType, request)      [module]
       feature guard · form-version guard · CheckPolicyAsync(Generate*)
              ↓
2  ApplicationGenerationQueue.QueueAsync                            [host]
       distributed lock  →  dedupe check  →  prerequisite validation
                         →  rate limit    →  insert AIGenerationRequest (Queued)
                         →  IBackgroundJobManager.EnqueueAsync
              ↓
3  AIGenerationBackgroundJob.ExecuteAsync                           [host]
       enter tenant scope · MarkRunning
              ↓
4  IAIGenerationOperationExecutor.ExecuteAsync                      [host]
       build input → call the module's operation service → persist result
              ↓
5  MarkCompleted (or MarkFailed + rethrow) · stamp the user's cooldown
```

## 1 · Submission

`AIGenerationAppService` (`api/app/ai/generation`) exposes one typed route per operation plus a generic `POST submit`. Every typed route delegates to `SubmitAsync`, which is the single funnel — the comment in the source says so explicitly: *"All generation routes converge here so authorization, feature, and form-version rules stay consistent."*

```csharp
var operation = AIGenerationOperations.Get(operationType);
await featureGuard.EnsureEnabledAsync(operation.FeatureName, operation.DisabledLocalizationKey);

if (operation.RequiresFormVersion && request.ApplicationFormVersionId is null)
    throw new UserFriendlyException($"AI operation '{operationType}' requires an application form version.");

await CheckPolicyAsync(operation.GeneratePermission);
await aiGenerationQueue.QueueAsync(operationType, request, currentTenant.Id);
```

`RequiresFormVersion` is true for exactly the three form-generation operations. The typed routes also carry their own `[Authorize(...)]` attribute, so the generate permission is checked twice — once declaratively, once inside `SubmitAsync`.

| Route | Operation type |
|---|---|
| `POST submit?operationType=…` | any |
| `POST attachment-summary` | `attachment-summary` |
| `POST application-analysis` | `application-analysis` |
| `POST application-scoring` | `application-scoring` |
| `POST form-mapping` | `form-mapping` |
| `POST form-worksheet` | `form-worksheet` |
| `POST form-scoresheet` | `form-scoresheet` |
| `GET status?applicationId=…&operationType=…` | any (checks the **View** permission) |

## 2 · Queueing

`ApplicationGenerationQueue.EnsureRequestAndEnqueueAsync` is where the concurrency rules live. The order is deliberate.

### The lock covers the dedupe check

```csharp
var requestLock = distributedLockProvider.CreateLock(
    $"ai-generation:{tenantId}:{request.ApplicationId}:{persistedOperation.Id}");

using (await requestLock.AcquireAsync())
{
    // existing Queued or Running request for this tenant+application+operation?
    if (existing != null) return;      // silently idempotent

    await validateInput();             // prerequisite validation
    await aiRateLimiter.EnsureAsync(currentUser.Id);

    // insert AIGenerationRequest (Queued), then enqueue the background job
}
```

The source comment is explicit about why the lock spans the check: *"The lock must cover the active-request check so each tenant/application/operation queues only once."* Without it, two clicks a millisecond apart would both see no active request and both enqueue.

A duplicate submission **returns quietly** — no exception, no second job, and no signal to the caller that nothing happened.

### Prerequisite validation

`AIGenerationPrerequisiteValidator.EnsureAvailableAsync` switches on the operation type and throws a localized `UserFriendlyException` when the operation cannot possibly succeed:

| Operation | Requires | Message key |
|---|---|---|
| `attachment-summary` | at least one CHEFS attachment on the application | `AI:NoAttachmentsAvailable` |
| `application-analysis` | a form submission with non-empty content | `AI:ApplicationAnalysisRequiresSubmission` |
| `application-scoring` | the form has a `ScoresheetId`… | `AI:ScoringRequiresScoresheet` |
| | …and that scoresheet has sections with fields | `AI:ScoringRequiresScoresheetFields` |
| `form-mapping` | the form version exists, and no `Active` review | `AI:FormMappingRequiresFormVersion`, `AI:FormGenerationReviewActive` |
| `form-worksheet` | same | `AI:FormWorksheetRequiresFormVersion`, `AI:FormGenerationReviewActive` |
| `form-scoresheet` | same | `AI:FormScoresheetRequiresFormVersion`, `AI:FormGenerationReviewActive` |

Validation happens **after** the dedupe check and **before** the rate limit, so a request that was going to be rejected anyway never consumes the user's cooldown.

### Rate limiting

`AIRateLimiter.EnsureAsync(userId)` throws `"AI generation is rate limited. Try again in N seconds."` if the user is inside their cooldown window.

- The cooldown lives in the **distributed cache** at `ai-generation:cooldown:{userId}`, holding an absolute expiry timestamp in ticks.
- Read-modify-write is wrapped in a distributed lock at `ai-generation:cooldown-lock:{userId}`.
- The window length is `Azure:Generation:CooldownSeconds`, which **must** be configured with a positive value — the getter throws `AbpException` otherwise.
- `userId` null (a background or system caller) **bypasses** the limit entirely, by design: *"No user (background/system flow). User-level rate limit does not apply."*
- The cooldown is stamped only **after a successful execution** (step 5), not at queue time — so a failed generation does not cost the user their next attempt.

`GetStateAsync` additionally reports `IsGenerating`, by asking every registered `IAIGenerationActivityProvider`. The host's `AIGenerationActivityProvider` answers by looking for a `Queued` or `Running` request created by the current user in the current tenant.

### Failure to enqueue

If `IBackgroundJobManager.EnqueueAsync` throws after the request row was inserted, the queue marks that row `Failed` on a best-effort basis and rethrows — so the row does not sit `Queued` forever blocking future submissions through the dedupe check.

## 3 · The background job

`AIGenerationBackgroundJob : AsyncBackgroundJob<AIGenerationBackgroundJobArgs>` owns tenant scope and the request lifecycle; the source comment draws the line: *"The job owns tenant scope and request lifecycle; executors own AI input and result persistence."*

```csharp
using var logScope = AIGenerationLogScope.Begin(logger, operationType, applicationId, tenantId, promptVersion, requestedByUserId);
using (currentTenant.Change(args.TenantId))
{
    await MarkRunningInNewUowAsync(...);
    try
    {
        var executor = operationExecutorRegistry.Resolve(args.OperationType);
        if (await executor.ExecuteAsync(args))
            await StampCooldownBestEffortAsync(...);      // only on a truthy result

        await MarkCompletedInNewUowAsync(...);
    }
    catch (Exception ex)
    {
        await MarkFailedInNewUowAsync(..., ex.Message, ...);
        throw;                                            // let ABP's job retry see it
    }
}
```

Every status transition runs in its **own short unit of work** (`requiresNew: true, isTransactional: false`). `AIGenerationRequestJobHelper` explains why that pattern is used for results too:

> Reloads the application in a fresh, short-lived unit of work and persists only the result mutated by `applyResult`. Keeping this load-to-save window short (rather than holding the aggregate loaded across a slow AI call) avoids `AbpDbConcurrencyException` when unrelated parts of the aggregate are modified concurrently.

This matters because a model call can take tens of seconds, and holding the `Application` aggregate loaded across it reliably produced concurrency exceptions.

`AIGenerationOperationExecutorRegistry.Resolve` pulls every registered `IAIGenerationOperationExecutor` from DI and picks the one whose `OperationType` matches exactly — `SingleOrDefault`, so two executors claiming the same type is a startup-time bug that surfaces as a runtime throw.

## 4 · Execution

Six executors under `GrantApplications/Automation/Operations/`. Each follows the same three beats:

1. **Build the input** — via `AIApplicationInputBuilder` (analysis, scoring) or the operation's own data provider (attachment summary, form generation). Inputs are assembled from the host's repositories through `IAIApplicationInputDataProvider`.
2. **Call the module** — `IApplicationAnalysisService`, `IApplicationScoringService`, `IAttachmentSummaryService`, `IFormMappingService`, `IFormWorksheetService`, `IFormScoresheetService`. All but the first three are implemented directly by `OpenAIRuntimeService`.
3. **Persist** — through `AIGenerationRequestJobHelper.SaveApplicationResultInNewUowAsync` or `SaveScoresheetAnswersInNewUowAsync`, or by writing a `GenerationReview`.

Where each result lands is documented in [ai-operations.md](ai-operations.md).

## 5 · Status

`GET api/app/ai/generation/status` checks the operation's **View** permission, then reads the latest request through `IAIGenerationStatusReader` → the host's `AIGenerationStatusAppService`. A missing request returns an empty `AIGenerationStatusDto` rather than a 404.

The DTO carries the same payload twice — once nested under `GenerationRequest` and once flattened onto the root — which is what lets the UI poll either shape. See [ai-roadmap.md](ai-roadmap.md#the-status-dto-carries-its-payload-twice).

## Triggers

### Manual

A user clicks Generate on the application detail page or the form mapping screen. Requires the feature, the `Generate*` permission, and — for the surfaces that check it — the tenant's `ManualGenerationEnabled` setting and the form's `ManuallyInitiateAIAnalysis` flag.

### Automatic, at intake

`QueueApplicationAIPipelineOnProcessHandler : ILocalEventHandler<ApplicationProcessEvent>` fires when an application is processed. It short-circuits, in order, on:

1. `AISettings.AutomaticGenerationEnabled` off for the tenant,
2. `ApplicationForm.AutomaticallyGenerateAIAnalysis` off for the form,
3. all three intake features (`AttachmentSummaries`, `ApplicationAnalysis`, `Scoring`) off.

Otherwise it calls `QueueApplicationIntakeAsync`, which queues the three application-level operations **independently**:

```text
AttachmentSummaries enabled? → queue attachment-summary
ApplicationAnalysis enabled? → queue application-analysis
Scoring             enabled? → queue application-scoring
```

Each is queued in its own `try`/`catch (UserFriendlyException)`. The method throws only if *no* feature was enabled, or if every enabled stage failed to queue — so an application with no attachments still gets analysis and scoring, and the attachment-summary failure is swallowed. The handler itself then catches and logs anything that escapes, so a failed AI pipeline never fails intake.

Note that the three stages are queued as **peers, not a sequence**. Nothing makes application analysis wait for attachment summaries to finish, even though analysis consumes attachment summaries as input — see [ai-roadmap.md](ai-roadmap.md#the-intake-pipeline-is-three-peers-not-a-chain).

### Downstream of scoring

When application scoring completes, the executor publishes `ApplicationAIScoringGeneratedEvent`. `CreateAIAssessmentOnScoringGeneratedHandler` picks it up, re-checks the `Unity.AI.Scoring` feature, and calls `AssessmentManager.CreateAiAssessmentAsync(application)` in its own unit of work — producing an AI assessment that sits alongside the human assessments rather than replacing one. Failures are logged and swallowed.
