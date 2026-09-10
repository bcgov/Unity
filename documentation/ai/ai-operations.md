# The Six AI Operations

Each operation has its own executor in `src/Unity.GrantManager.Application/GrantApplications/Automation/Operations/`, deriving from `AIGenerationOperationExecutor` and declaring an `OperationType`. The registry resolves them by that string.

An executor returns `bool`. Returning **`true`** stamps the requesting user's rate-limit cooldown; returning **`false`** completes the request without charging the user — used when the executor decided there was nothing to do.

## At a glance

| Operation | Input | Output lands in | Feature | Needs form version |
|---|---|---|---|---|
| `attachment-summary` | Attachment file content, text-extracted | Each attachment's summary field | `Unity.AI.AttachmentSummaries` | no |
| `application-analysis` | Submission + form schema + attachment summaries | `Application.AIAnalysis` | `Unity.AI.ApplicationAnalysis` | no |
| `application-scoring` | Submission + attachment summaries + scoresheet schema | `AI.ApplicationScoresheetAnswers` (tenant DB) | `Unity.AI.Scoring` | no |
| `form-mapping` | CHEFS fields + Unity core fields + existing mapping | `GenerationReview` payload | `Unity.AI.FormMapping` | **yes** |
| `form-worksheet` | Form schema + mapping read model + existing worksheets | A draft Flex worksheet + `GenerationReview` | `Unity.AI.FormWorksheet` | **yes** |
| `form-scoresheet` | Form schema + mapping read model | A draft Flex scoresheet + `GenerationReview` | `Unity.AI.FormScoresheet` | **yes** |

The first three act on an **application**; the last three act on an **application form version**. That split is what `AIGenerationOperationDefinition.RequiresFormVersion` encodes.

## Attachment summary

The only operation that reads binary content.

```text
AttachmentSummaryOperationExecutor
    → IAttachmentSummaryService.GenerateForApplicationAsync(applicationId, promptVersion, attachmentIds)
          for each attachment (fan-out per AIExecutionMode):
              IAttachmentContentProvider.OpenAsync            → stream + content type
              ITextExtractionService.ExtractTextAsync         → plain text, capped at 50,000 chars
              IAIService.GenerateAttachmentSummaryAsync       → summary
              SaveSummaryAsync                                → persisted on the attachment
```

`AttachmentIds` on the submission DTO narrows the run to specific attachments; empty means all of them. The prerequisite validator requires at least one CHEFS attachment on the application.

Two failure paths both end with a **sentinel string persisted as the summary**:

| Situation | Stored summary |
|---|---|
| Text extraction returned nothing (including every `.doc` file) | `"Attachment text could not be extracted for AI summary generation."` |
| The model call failed or returned invalid output | `"AI analysis not available for this attachment ({fileName})."` |

The generation request still completes successfully. See [ai-roadmap.md](ai-roadmap.md#failures-are-persisted-as-content).

The model is asked for JSON and the runtime extracts the `summary` property; if the response is not a JSON object with a string `summary`, the raw trimmed output is used instead.

## Application analysis

```text
ApplicationAnalysisOperationExecutor
    → objectMapper.Map<Application, AIApplicationPromptDataDto>
    → AIApplicationInputBuilder.BuildApplicationAnalysisInputAsync
          submission           ← IAIApplicationInputDataProvider.GetApplicationSubmissionAsync
          attachment summaries ← GetAttachmentSummariesAsync
          form schema          ← form version's FormSchema
          Schema  = PromptDataPayloadBuilder.BuildFormFieldConfiguration(formSchema)
          Data    = PromptDataPayloadBuilder.BuildPromptDataPayload(application, submission, formSchema)
    → ApplicationAnalysisService.RegenerateAsync
    → SaveApplicationResultInNewUowAsync(app => app.AIAnalysis = analysisJson)
```

Prompt placeholders: `{{SCHEMA}}`, `{{DATA}}`, `{{ATTACHMENTS}}`. Attachments are passed as `{ name, summary }` pairs — the analysis reads the *summaries*, not the documents, which is why it is queued alongside attachment summarisation rather than after it.

This is the **only** operation that turns a provider failure into a thrown `UserFriendlyException`, so it is the only one where a failed model call marks the generation request `Failed` and shows the user an error.

Result shape is validated by `ValidateApplicationAnalysisJson` and parsed by `OpenAIResponseParser.ParseApplicationAnalysisResponse` into `ApplicationAnalysisFinding` items.

## Application scoring

The most involved operation, because it fans out over scoresheet sections and has to survive the model inventing question ids.

```text
ApplicationScoringOperationExecutor
    → AIApplicationInputBuilder.BuildApplicationScoringInputAsync
          requires form.ScoresheetId, and a scoresheet with sections that have fields
          sections ← scoresheet.Sections ordered by Order, each projected to
                     [{ id, section, question, description, type, options, allowed_answers }]
    → ApplicationScoringService.RegenerateAsync
          mode ← AIExecutionModeResolver.ResolveModeAsync("ApplicationScoring")
          AIExecutionStrategy.RunAsync(sections, mode, perSection, batch)
    → merge every section's answers into one dictionary → JSON
    → SaveScoresheetAnswersInNewUowAsync   (upsert one row per application)
    → publish ApplicationAIScoringGeneratedEvent
```

### Question-id aliasing

Scoresheet question ids are GUIDs. Before the section goes into the prompt, `OpenAIPromptRenderer.BuildAliasedApplicationScoringSection` swaps them for short tokens and hands back a `questionIdAliasMap`; `OpenAIResponseParser.ParseApplicationScoringResponse` maps the answers back. This keeps the model from mangling long identifiers and makes the response template compact.

If the response template builds to `"{}"` — the section schema produced nothing usable — the service logs a warning and returns empty **without calling the model**.

### Execution modes

`AIExecutionStrategy.RunAsync` implements all three modes for any multi-item operation:

| Mode | Behaviour |
|---|---|
| `Sequential` | One section at a time, in order. The seeded default. |
| `Parallel` | `Task.WhenAll` over every section, all in flight at once — no concurrency cap. |
| `Batch` | One call for all sections: `BuildBatchSectionSchema` flattens every section's questions into a single array and sends it as section `"All Sections"`. |

The mode is a column on `AIOperation`, so it is changed in data. A `Batch` run trades many small calls for one large one, which is what the 8000-token budget on `ApplicationScoring` is sized for.

### Partial results are saved as complete

`ProcessSectionAsync` catches per-section exceptions, logs them, and returns an empty dictionary for that section. The merge then produces a scoresheet missing those sections' answers, and the request completes successfully. See [ai-roadmap.md](ai-roadmap.md#a-partly-failed-scoring-run-is-saved-as-if-complete).

### Downstream

`ApplicationAIScoringGeneratedEvent` → `CreateAIAssessmentOnScoringGeneratedHandler` → `AssessmentManager.CreateAiAssessmentAsync(application)`. The handler re-checks the `Unity.AI.Scoring` feature, runs in its own unit of work, and logs-and-swallows any failure. The result is an **AI assessment alongside the human ones**, not a modification of anyone's assessment.

## Form mapping

Suggests mappings from CHEFS form fields onto Unity fields, for a given application form version.

```text
FormMappingOperationExecutor
    → IApplicationFormVersionMappingReadService.GetAsync(formVersionId)   → read model
    → if an Active GenerationReview exists → return false (nothing done, no cooldown charged)
    → IFormMappingService.GenerateFormMappingAsync
    → FailureReason set? → throw InvalidOperationException (request marked Failed)
    → open or reuse a GenerationReview, sequence = previous + 1
    → FormMappingResponseMapper.ParseSuggestions(response.Mapping)
    → payload.PendingSuggestions = suggestions
    → no suggestions? review.Complete()
```

### Initial and final passes alternate by sequence

```csharp
var isFinalMapping = review.Sequence > 1 && review.Sequence % 2 == 0;
```

Odd sequences are **initial** mapping passes; even sequences from 2 onward are **final** passes, which additionally run `ClassifyFinalSuggestions` against the existing mapping to separate genuinely new suggestions from ones that leave the mapping unchanged (counted into `UnchangedSuggestionCount`).

That alternation is the mapping half of the guided workflow whose steps are named in `AILocalizationKeys`:

```text
WorkflowGenerateInitialMapping → WorkflowReviewInitialMapping
    → WorkflowGenerateWorksheets → WorkflowReviewWorksheets
    → WorkflowPublishAssignWorksheets
    → WorkflowGenerateFinalMapping → WorkflowReviewFinalMapping → WorkflowCompleted
```

The intent: map what you can, generate worksheets for what you cannot, publish and assign those worksheets, then map again now that the new custom fields exist.

## Form worksheet

Generates a draft Flex worksheet from a form version's schema.

```text
FormWorksheetOperationExecutor
    → if an Active GenerationReview exists → return false
    → baseWorksheetName = AiWorksheetSuggestionName.Build(formVersionId)
    → an AI suggestion worksheet already exists?
          log "leaving it unchanged" and skip generation
      otherwise:
          promptData = { applicationFormVersionId, chefsFormVersionGuid, applicationFormId,
                         formName, scoresheetId, chefsFields, unityCoreFields,
                         existingMapping, formSchema, existingCustomFields }
          → IFormWorksheetService.GenerateFormWorksheetAsync
          → create the draft worksheet + a GenerationReview
```

The generated worksheet is written under a **canonical, deterministic name** derived from the form version id, and `EnsureCanonicalSuggestionWorksheetState` asserts its expected state. That name is what makes the operation idempotent: a second run finds the existing suggestion worksheet and leaves it alone rather than producing a duplicate.

`existingCustomFields` is included in the prompt so the model does not re-propose fields that already exist on other worksheets.

Failure returns `Worksheet = "{}"` with a `FailureReason` rather than throwing.

The localization keys around this operation describe the review rules: `AI:FormWorksheetDeleteProtected`, `AI:WorksheetDraftsMustBePublished`, `AI:WorksheetTitleRequired`, `AI:WorksheetSelectionRequired`.

## Form scoresheet

The same shape as form worksheet, producing a draft Flex scoresheet.

Its validation is the most elaborate of the six, judging by the dedicated localization keys — `AI:ScoresheetGenerationInvalidOutput`, `Empty`, `Unusable`, `NoVersion`, `NoSections`, `SectionNoFields`, `PropertyInvalid`, `Protected`, `RequiresFormVersion`. A generated scoresheet with no sections, or a section with no fields, is rejected as unusable rather than saved as an empty draft.

Note the runtime asymmetry: `GenerateFormScoresheetAsync` returns `Scoresheet = AIResponseJson.CleanJsonResponse(result.Content)` **without checking the outcome first**, unlike its worksheet sibling which returns `"{}"` on failure. Both carry `FailureReason`. See [ai-roadmap.md](ai-roadmap.md#form-scoresheet-does-not-check-its-outcome).

## The review contract

All three form-generation operations write a `GenerationReview` rather than committing anything to the form:

- `Operation` — the operation type, `ContextId` — the form version id, `Sequence` — the pass number. Unique together.
- `Status` — `Active` while a human still has to decide, then `Completed` or `Discarded`.
- `ReviewData` — a `jsonb` payload holding the pending suggestions and their classification.

An `Active` review is a **lock**: the prerequisite validator refuses to queue another run of that operation for that form version (`AI:FormGenerationReviewActive`), and the executor double-checks and returns `false` if one appeared in the meantime.

This is the mechanism that keeps AI out of the form configuration itself. Nothing the model produces reaches an application form until a person completes the review.
