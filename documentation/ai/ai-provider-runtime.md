# AI Provider Runtime

Everything under `Unity.AI.Application/Runtime/Execution/`. This is the layer that turns "score this application" into an HTTPS request and a validated JSON string.

## The provider is Azure OpenAI

`OpenAIChatClientFactory` is the whole client construction:

```csharp
public ChatClient Create(OpenAIOperationSettings settings) =>
    new AzureOpenAIClient(
        settings.Endpoint,
        new ApiKeyCredential(settings.ApiKey),
        new AzureOpenAIClientOptions())
        .GetChatClient(settings.DeploymentName);
```

So: **Azure OpenAI**, key-based auth, one deployment per call. The `Provider` string stored on `AIModel` and checked by the transport is nevertheless `"OpenAI"` — the name refers to the model family, not the hosting service. Anything else is rejected before a request is made:

```csharp
if (!string.Equals(settings.ProviderName, "OpenAI", StringComparison.Ordinal))
    return AIOperationResult.PermanentFailure(new AIProviderResult($"Unsupported provider: {settings.ProviderName}"));
```

## Resolving what to call

`OpenAIConfigurationResolver.ResolveOperationSettingsAsync(operationName)` produces the `OpenAIOperationSettings` record that drives everything downstream. It is the only resolution path the runtime actually uses.

```text
cache hit on "ai:operation-settings:{tenantId|host}:{operation}"?  → return (5 min TTL)
        ↓
AIOperation by name, IsActive               → else InvalidOperationException "not configured"
        ↓
AIModel by operation.AIModelId              → else InvalidOperationException "is inactive"
        ↓
model.SettingsJson → AIModelSettings        → else "has invalid settings JSON"
        ↓
model.Provider must equal "OpenAI"          → else "provider is not supported"
        ↓
configuration["Azure:OpenAI:Endpoint"]      → required, must be an absolute URI
        ↓
active AIPrompt for the operation name      → else "has no active prompt for the current tenant"
        ↓
operation.CompletionTokens > 0              → else "must define a positive CompletionTokens value"
        ↓
configuration["Azure:OpenAI:ApiKey"]        → required
```

The resulting record:

```csharp
new OpenAIOperationSettings(
    ProviderName:                 "OpenAI",
    ProfileName:                  model.Name,
    ApiKey:                       configuration["Azure:OpenAI:ApiKey"],
    Endpoint:                     new Uri(configuration["Azure:OpenAI:Endpoint"]),
    DeploymentName:               model.Name,          // same as ProfileName
    MaxOutputTokenCountSupported: modelSettings.MaxOutputTokenCountSupported,
    Temperature:                  modelSettings.Temperature,
    CompletionTokens:             operation.CompletionTokens,
    PromptVersion:                $"v{prompt.VersionNumber}");
```

Both `ProfileName` and `DeploymentName` are `model.Name`, so the **Azure deployment must be named after the model** (`gpt-5-mini`, `gpt-4o-mini`, `gpt-5-nano`).

### Caching

Two five-minute `IMemoryCache` entries per operation, keyed by tenant:

| Key | Holds |
|---|---|
| `ai:operation-snapshot:{tenantId\|host}:{operation}` | A `ResolvedOperationSnapshot` — id, name, model id, token budget, active flag |
| `ai:operation-settings:{tenantId\|host}:{operation}` | The whole `OpenAIOperationSettings`, **including the API key** |

`IMemoryCache` is registered by `AIApplicationModule` (`AddMemoryCache()`), so this is per-pod and not invalidated by writes. Changing a model, token budget, or prompt activation takes effect within five minutes, independently on each pod. See [ai-roadmap.md](ai-roadmap.md#configuration-caching-is-per-pod-and-holds-the-api-key).

### `IsAvailableAsync`

`OpenAIRuntimeService.IsAvailableAsync` is a shallow check: it calls `ResolveApiKeyAsync`, which only reads `Azure:OpenAI:ApiKey` from configuration. It does not verify the endpoint, the model, the operation, or reachability — a configured-but-wrong key reports available.

## Resolving the prompt

The runtime asks for a prompt by family and version:

```csharp
var settings       = await resolver.ResolveOperationSettingsAsync(promptType, ct);
var promptTemplate = await promptTemplateProvider.GetRequiredPromptAsync(
    promptType,
    request.PromptVersion ?? settings.PromptVersion,   // caller override wins
    ct);
```

So a caller may pin a prompt version per request (`AIGenerationSubmissionDto.PromptVersion` flows all the way through the job args), falling back to whatever the resolver picked as the active version. `AIPromptTemplateStore` then loads the row with the multi-tenancy filter disabled, preferring the current tenant's row over the global one. Details in [ai-prompts.md](ai-prompts.md).

## The transport

`OpenAITransportService.GenerateSummaryAsync(content, systemPrompt, settings, maxTokens, ct)` is the single method every operation goes through.

### Message shape

Two messages, always:

```csharp
new SystemChatMessage(systemPrompt ?? "You are a professional grant analyst for the BC Government.")
new UserChatMessage(content ?? string.Empty)
```

The fallback system prompt is a hard-coded string in the transport — it applies only when a prompt row has an empty `SystemPrompt`.

### Options are built defensively

```csharp
if (settings.MaxOutputTokenCountSupported) options.MaxOutputTokenCount = maxTokens;
if (includeTemperature && settings.Temperature.HasValue) options.Temperature = (float)settings.Temperature.Value;
```

Both parameters are omitted rather than defaulted when the model does not support them — which is why `gpt-5-mini` and `gpt-5-nano` are seeded with `MaxOutputTokenCountSupported = false` and no temperature.

### The temperature fallback

Newer model families reject `temperature` outright. Rather than encode a model-family table, the transport retries once without it:

```text
CompleteChatAsync(..., includeTemperature: true)
        ↓ ClientResultException
status == 400  AND  body mentions "temperature"
               AND  body mentions "unsupported" | "not supported" | "not allowed" | "invalid"
        ↓ yes
CompleteChatAsync(..., includeTemperature: false)
```

Any other failure rethrows. The retry is logged at warning level with the profile name.

### Reading the response

The transport reads the model output twice over, because the SDK's typed content is not always populated:

1. Concatenate every non-blank `completion.Content[i].Text`.
2. If that yields nothing, parse the raw response body and pull `choices[0].message.content` — handling both the string form and the array-of-parts form.

It separately parses the raw body for provider metadata — `model`, `choices[0].finish_reason`, and `usage.prompt_tokens` / `completion_tokens` / `total_tokens` / `completion_tokens_details.reasoning_tokens` — into `AIProviderResult`. An empty model output is logged with all of it, which is what makes a truncation (`finish_reason: length`) or a reasoning-token blowout diagnosable after the fact.

### Outcome mapping

| Situation | Outcome |
|---|---|
| Non-empty model output | `Success` |
| HTTP 200 but empty output | `InvalidOutput` |
| `ClientResultException` 408, 429, or ≥ 500 | `TransientFailure` |
| `ClientResultException` any other status | `PermanentFailure` |
| `ClientResultException` with no status | `TransientFailure` |
| `InvalidOperationException` (bad configuration) | `PermanentFailure` |
| Any other exception | `TransientFailure` |
| Unsupported provider name | `PermanentFailure` |

`OperationCanceledException` is always rethrown, never mapped.

## The retry-and-validate loop

`OpenAIRuntimeService.GenerateWithRetryAsync` wraps the transport with `MaxAiAttempts = 3`:

```text
for attempt in 1..3
    result = await transport(...)

    if Success:
        validation = validator(result.Content)
        if valid  → return result
        else      → result = result.WithOutcome(InvalidOutput, category, reason); log warning

    if PermanentFailure → return immediately        (no point retrying a 401)

    if attempt < 3 → log and loop
return last result
```

Two things make this different from an ordinary HTTP retry:

- **A 200 is not success.** The payload has to validate against the operation's expected shape before the attempt counts.
- **There is no backoff.** Attempts follow one another immediately, including after a 429. See [ai-roadmap.md](ai-roadmap.md#the-retry-loop-has-no-backoff).

### Validators

`AIProviderPayloadValidator` provides one per operation, each returning `AIResponseValidationResult` — valid, or invalid with a category and a human-readable reason that goes into the log:

| Validator | Checks |
|---|---|
| `ValidateAttachmentSummaryText` | Non-empty text |
| `ValidateApplicationAnalysisJson` | Parses as JSON with the expected analysis shape |
| `ValidateApplicationScoringJson(response, sectionJson)` | Parses, and the answers correspond to the questions in the section that was asked |
| `ValidateFormMappingJson` | Parses as the mapping shape |
| `ValidateFormWorksheetJson` | Parses as the worksheet shape |
| `ValidateFormScoresheetJson` | Parses as the scoresheet shape |

`AIResponseJson.CleanJsonResponse` strips markdown fencing before parsing — a leading ```` ```json ```` (or bare ```` ``` ````) and a trailing ```` ``` ```` — because models return fenced JSON regardless of instructions.

## Outcome taxonomy

```csharp
enum AIOperationOutcome  { Success, TransientFailure, PermanentFailure, InvalidOutput }
enum AIFailureCategory   { None, ProviderUnavailable, TransientProviderFailure,
                           PermanentProviderFailure, InvalidOutput }
```

`AIOperationResult` is a record carrying the outcome, the `AIProviderResult`, a failure category and an optional reason, with static factories (`Success`, `TransientFailure`, `PermanentFailure`, `ProviderUnavailable`, `InvalidOutput`) and `WithOutcome(...)` for the downgrade the validator performs.

## What each operation does with a failure

This is where the operations diverge sharply, and it matters for anyone debugging a "the AI did nothing" report:

| Operation | On non-success |
|---|---|
| **Application analysis** | Logs the outcome, failure category, HTTP status and the first 400 characters of the provider response, then throws `UserFriendlyException("Application analysis generation failed.")` — the job fails and the request is marked `Failed`. |
| **Attachment summary** | Returns a summary of `"AI analysis not available for this attachment ({fileName})."` — a **successful** result containing a failure message, which is then persisted as the summary. |
| **Application scoring** | Returns an empty `ApplicationScoringResponse()` — no answers, no error. |
| **Form worksheet** | Returns `Worksheet = "{}"` with a populated `FailureReason`. |
| **Form scoresheet** | Returns the cleaned content and whatever `FailureReason` the result carried — note it does **not** check the outcome first. |
| **Form mapping** | Returns a `FormMappingResponse` with `FailureReason` set and no mapping. |

Only application analysis surfaces the failure as an exception. See [ai-roadmap.md](ai-roadmap.md#failures-are-persisted-as-content).

## Prompt logging

`OpenAIPromptFileLogger` writes the full rendered prompt and the raw model output to a local file:

- Off unless `Azure:Logging:EnablePromptFileLog` is `true`.
- Path: `{AppContext.BaseDirectory}/logs/ai-prompts-{yyyyMMdd-HHmmss}-{processId}.log`, appended.
- Each entry: timestamp, prompt type, prompt version, `INPUT` (system prompt + user prompt) or `OUTPUT` (raw provider response).
- Failures to write are logged as warnings and swallowed.

It is called on **every** operation, both before the call and after it. When enabled it therefore writes applicant submission data and attachment text to container-local disk — useful for prompt engineering, and worth knowing before enabling it anywhere real.

## Text extraction

`TextExtractionService` (`Unity.AI.Application/Extraction/`) turns an attachment stream into the plain text that goes into the attachment-summary prompt. It runs **in-process**; there is no document-intelligence service.

Dispatch is by file extension first, then by content type:

| Extension | Library |
|---|---|
| `.txt` `.csv` `.json` `.xml` | Direct read |
| `.pdf` | `UglyToad.PdfPig` |
| `.docx` | `NPOI.XWPF` |
| `.xls` `.xlsx` | `NPOI.SS` |
| `.pptx` | OpenXML over `System.IO.Compression` |
| `.doc` | **Returns empty string immediately** — legacy Word is not supported |
| anything else | Falls back to matching the content type against `text/`, `pdf`, `word`/`msword`/`officedocument.wordprocessingml`, `excel`/`spreadsheet`, `presentation`/`powerpoint` |

Hard caps guard against a pathological document consuming the whole token budget:

| Limit | Value |
|---|---|
| `MaxExtractedTextLength` | 50,000 characters |
| `MaxExcelSheets` | 10 |
| `MaxExcelRowsPerSheet` | 2,000 |
| `MaxExcelCellsPerRow` | 50 |
| `MaxDocxParagraphs` | 2,000 |
| `MaxDocxTableRows` | 2,000 |
| `MaxDocxTableCellsPerRow` | 50 |
| `MaxPowerPointSlides` | 200 |

Any extraction exception is logged and returns an empty string, which the attachment-summary service treats as "text could not be extracted".
