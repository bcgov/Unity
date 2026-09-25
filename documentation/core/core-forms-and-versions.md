# Core — Forms and Versions

An `ApplicationForm` is a configured intake channel bound to a CHEFS form. An `ApplicationFormVersion` is one published version of that CHEFS form, plus the mapping that turns its submissions into Unity fields.

Together they are the most consequential configuration in the product: a dozen flags on the form change how the application workflow, payments, AI and the applicant portal behave.

## `ApplicationForm`

`Domain/Applications/ApplicationForm.cs` — `FullAuditedAggregateRoot<Guid>`, `IMultiTenant`.

### CHEFS binding

| Property | Purpose |
|---|---|
| `ChefsApplicationFormGuid` | The CHEFS form this intake channel listens to — how a webhook is routed to a form |
| `ChefsCriteriaFormGuid` | An optional second CHEFS form for criteria |
| `ApiKey` | Stored encrypted; decrypted when calling CHEFS |
| `AvailableChefsFields` | The field inventory pulled from CHEFS, used to build mappings |
| `ConnectionHttpStatus`, `AttemptedConnectionDate` | Last connectivity check, surfaced on the form configuration screen |
| `IntakeId`, `Version`, `Category` | Grouping and versioning metadata |

### Flags that change product behaviour

This is the part worth knowing by heart — each of these reaches into a different part of the system:

| Flag | Effect | Documented in |
|---|---|---|
| `IsDirectApproval` | Unlocks `Approve`/`Deny` from any open state, bypassing review and assessment entirely | [core-application-lifecycle.md](core-application-lifecycle.md#direct-approval) |
| `ScoresheetId` | Which Flex scoresheet an assessment instantiates | [core-assessment.md](core-assessment.md#creating-an-assessment) |
| `Payable` | Whether payments can be raised against applications on this form | [`payments/`](../payments/README.md) |
| `PreventPayment` | Approved payments route to FSB as a spreadsheet instead of to CAS | [`payments/payments-approval-workflow.md`](../payments/payments-approval-workflow.md) |
| `AccountCodingId` | Default GL coding for payments on this form | [`payments/payments-domain-model.md`](../payments/payments-domain-model.md) |
| `PaymentApprovalThreshold` | Combined with the user threshold to decide whether L3 approval is needed | [`payments/payments-approval-workflow.md`](../payments/payments-approval-workflow.md#where-the-threshold-comes-from) |
| `DefaultPaymentGroup` | EFT or cheque for newly created supplier sites | [`payments/payments-suppliers-and-sites.md`](../payments/payments-suppliers-and-sites.md) |
| `FormHierarchy`, `ParentFormId` | Marks the form as `Parent` or `Child`; validated against real application links before payment | [core-application-lifecycle.md](core-application-lifecycle.md#application-links) |
| `AutomaticallyGenerateAIAnalysis` | The AI intake pipeline may fire for this form | [`ai/ai-generation-pipeline.md`](../ai/ai-generation-pipeline.md#triggers) |
| `ManuallyInitiateAIAnalysis` | Staff may trigger AI generation on this form | [`ai/ai-web-ui.md`](../ai/ai-web-ui.md) |

### Identifier generation

`Prefix` (max 100 chars) plus `SuffixType` produce the `UnityApplicationId` at intake:

```csharp
public enum SuffixConfigType { SequentialNumber, SubmissionNumber }
```

`GetAvailableSuffixTypes()` supplies the display names, and `SetSuffixType` validates with `Enum.IsDefined` and throws `ArgumentOutOfRangeException`. Both being null means no Unity ID is generated — see [core-intake.md](core-intake.md#the-unity-application-id).

### Electoral district source

`ElectoralDistrictAddressType` (default `PhysicalAddress`) decides which applicant address the electoral district is derived from at intake. Same pattern: `GetAvailableElectoralDistrictAddressTypes()`, a validating `SetElectoralDistrictAddressType`, and a static `GetDefaultElectoralDistrictAddressType()`.

### External links

`ExternalLinksConfig` holds links shown to applicants in the portal, with a hard cap:

```csharp
public const int MaxRelatedExternalLinks = 8;
```

`SetExternalLinks(renewalLink, relatedLinks, applicantMessage)` replaces the whole set atomically and enforces three rules:

| Rule | Error code |
|---|---|
| At most 8 related links | `TooManyRelatedLinks` |
| A published renewal link must have a URI | `RenewalLinkRequiredForVisibility` |
| A published related link must have a URI | `RelatedLinkInvalidUri` |

The method stamps `ExternalLinkType.Renewal` on the renewal link and `ExternalLinkType.Related` on the rest, so callers cannot mislabel them. The intent of the validation is that **nothing can be marked visible to applicants without somewhere to go.**

## `ApplicationFormVersion`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. One row per CHEFS form version.

| Property | Purpose |
|---|---|
| `ChefsApplicationFormGuid`, `ChefsFormVersionGuid`, `Version`, `Published` | CHEFS identity |
| `SubmissionHeaderMapping` | **The field map** — JSON, CHEFS field → Unity field |
| `AvailableChefsFields` | The field inventory for this version |
| `FormSchema` | The raw CHEFS schema as `jsonb`; what AI form-generation reads |
| `ReportColumns`, `ReportKeys`, `ReportViewName` | Metadata of the deprecated auto-generated reporting views; no longer written, dropped by the planned Phase 2 migration — see [`reporting/reporting-auto-generated-views.md`](../reporting/reporting-auto-generated-views.md) |

`HasSubmissionHeaderMapping(field)` deserialises the mapping to a `Dictionary<string, string>` and reports whether a field is mapped, swallowing malformed JSON as `false`.

### The mapping is per version, not per form

This is the single most important thing about form versions. At intake:

```text
submission.formVersionId
      ↓
ApplicationFormVersion where ChefsFormVersionGuid == formVersionId
      ↓
SubmissionHeaderMapping  →  IIntakeFormSubmissionMapper.MapFormSubmissionFields
```

A submission arriving for a CHEFS version Unity has never seen resolves to no mapping, and the mapper falls back to defaults — the application is still created, but only the fields the defaults cover are populated. Publishing a new version in CHEFS without mapping it in Unity therefore degrades quietly rather than failing.

## Synchronising with CHEFS

`ApplicationFormSycnronizationService` (note the misspelled type name) owns the pull side.

| Method | Role |
|---|---|
| `GetConnectedApplicationFormsAsync()` | Forms with usable CHEFS credentials |
| `GetChefsSubmissions(form, numberOfDaysToCheck)` | Submission ids CHEFS holds in the window |
| `GetSubmissionsByFormAsync(formId)` | Submission ids Unity holds |
| `GetMissingSubmissions(numberOfDaysToCheck)` | The set difference, plus a human-readable report |
| `SynchronizeFormSubmissions(...)` | Re-imports what is missing |
| `GetSubmissionsList(form, queryString)` | `GET {chefsApi}/forms/{formGuid}/submissions` with basic auth |

Two details:

- **Requests are paced.** `ChefsRequestPacingDelay = TimeSpan.FromMilliseconds(500)` between calls, so a sweep across many forms does not hammer CHEFS.
- **Auth is the form's own API key**, decrypted and used as basic auth with the CHEFS form GUID as the username.

`GetMissingSubmissions` is what `IntakeSyncWorker` calls twice a day, and it also posts a "Review Missed Chefs Submissions" activity notification per tenant. See [core-intake.md](core-intake.md#the-nightly-resync).

## `ApplicationFormVersionAppService`

At **1,258 lines** this is the second-largest service in the core, and it does two distinct jobs.

### Version lifecycle

| Method | Role |
|---|---|
| `InitializePublishedFormVersion(chefsForm, applicationFormId, initializePublishedOnly)` | Creates version rows from a CHEFS form payload |
| `TryInitializeApplicationFormVersion(...)`, `TryInitializeApplicationFormVersionWithToken(...)` | Best-effort single-version initialisation |
| `UpdateOrCreateApplicationFormVersion(...)` | Upsert |
| `FormVersionExists(chefsFormVersionId)`, `GetByChefsFormVersionId(...)`, `GetFormVersionSubmissionMapping(...)` | Lookups used by intake |
| `GetFormVersionByApplicationIdAsync(applicationId)` | The version behind an existing application |
| `DeleteWorkSheetMappingByFormName(formName, formVersionId)` | Removes a Flex worksheet mapping |

### The AI-assisted mapping workflow

The larger half. AI generates mapping suggestions, draft worksheets and draft scoresheets; this service is where a human accepts or discards them, and it is the counterpart to the `GenerationReview` records the AI module writes.

| Method | Role |
|---|---|
| `GenerateMappingAsync(id)` | Produce a mapping for review |
| `GetMappingReviewAsync(formVersionId)` | The current review and its pending suggestions |
| `AcceptMappingSuggestionsAsync(...)` | Apply chosen suggestions to `SubmissionHeaderMapping` |
| `DiscardMappingSuggestionsAsync(formVersionId)` | Throw the suggestions away |
| `SetMappingReviewPhaseAsync(formVersionId, FormMappingReviewPhase)` | Advance the guided workflow |
| `FinalizeMappingReviewAsync(formVersionId)` | Close the review |
| `ResetAiFlowAsync(formVersionId)` | Start over |
| `GetPendingAiWorksheetAsync` / `CreateAiWorksheetDraftAsync` / `DiscardAiWorksheetSuggestionsAsync` | Draft worksheet review |
| `GetPendingAiScoresheetAsync` / `CreateAiScoresheetDraftAsync` / `DiscardAiScoresheetSuggestionsAsync` | Draft scoresheet review |

The phases are `FormMappingReviewPhase` (`Domain.Shared/ApplicationForms/Mapping/`), and the guided sequence — generate initial mapping, review, generate worksheets, review, publish and assign, generate final mapping, review, complete — is described from the AI side in [`ai/ai-operations.md`](../ai/ai-operations.md#the-guided-mapping-workflow).

**Nothing AI produces reaches a form without passing through one of these accept methods.**

## The rest of the form services

| Service | Role |
|---|---|
| `ApplicationFormAppService` | CRUD over forms, the per-form configuration flags, and `GetFormDetailsByApplicationIdAsync` / `GetFormPaymentApprovalThresholdByApplicationIdAsync` used by Payments |
| `ApplicationFormConfigurationAppService` | The configuration screens' backing service |
| `ApplicationFormTokenAppService` | CHEFS API key handling |
| `FormController` (`HttpApi`) | `POST api/app/form/{formId}/version/{formVersionId}` — synchronise available fields for a version |

## Where forms surface

- **Form configuration** (`Web/Pages/FormConfiguration/`, `Web/Pages/ApplicationForms/`) — the flags, CHEFS connection, external links, AI configuration widget.
- **Mapping screen** (`Web/Pages/ApplicationForms/Mapping.cshtml`) — the AI-assisted mapping review.
- **Grant programs** (`Web/Pages/GrantPrograms/`) — forms grouped into programs.
- **Payment configuration widget** — the payment flags, owned by the core but rendering Payments concepts.
