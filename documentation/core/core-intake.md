# Core — Intake

Intake is how a CHEFS form submission becomes a Unity `Application`. It is the only path by which applications are created — there is no manual "create application" screen.

```text
CHEFS ──webhook──▶ POST api/chefs/event/{__tenant}          [AllowAnonymous]
                        ↓
                   IntakeSubmissionAppService
                        resolve ApplicationForm by CHEFS form GUID
                        fetch the full submission back from the CHEFS API
                        validate (reject drafts and deleted submissions)
                        store the CHEFS field mapping for this form version
                        ↓
                   IntakeFormSubmissionManager.ProcessFormSubmissionAsync
                        map submission → IntakeMapping
                        ── transactional unit of work ──
                        create Application (+ Unity Application ID)
                        create or retrieve Applicant, ApplicantAgent, default supplier
                        save CHEFS file attachments
                        insert ApplicationFormSubmission
                        map and persist Flex custom fields
                        publish ApplicationProcessEvent
                        publish ApplicationChangedEvent (Submit)
                        ↓
                   handlers: electoral district · profile cache · report data · AI pipeline
```

## The entry point

`EventSubscriptionController` (`HttpApi/Controllers/`) is `[AllowAnonymous]` and exposes two routes:

| Route | Use |
|---|---|
| `POST api/chefs/event` | Untenanted |
| `POST api/chefs/event/{__tenant}` | Tenant-resolved from the route segment |

CHEFS posts an `EventSubscription` body identifying a form and a submission. The payload is **not trusted as data** — Unity uses only the identifiers and then fetches the submission back from the CHEFS API itself, authenticated with the form's stored API key.

`SubmissionAppService` and `FormController` (`POST api/app/form/{formId}/version/{formVersionId}`) provide the pull-side counterparts: reading a submission on demand and synchronising a form version's available fields.

## Validation before anything is created

`IntakeSubmissionAppService.CreateIntakeSubmissionAsync`:

1. **Resolve the form.** Finds the `ApplicationForm` whose `ChefsApplicationFormGuid` matches, ordered by `CreationTime`, taking the **first**. No match throws `ApplicationFormSetupException("Application Form Not Registered")`.
2. **Fetch the submission** via `IFormsApiService.GetSubmissionDataAsync(formId, submissionId)`. A null response throws `InvalidFormDataSubmissionException`.
3. **Validate.** Two rejections, both of which notify a Teams channel rather than throwing:
   - `submission.draft == True` — "A draft submission was submitted and should not have been"
   - deleted submissions
   Both return an `EventSubscriptionConfirmationDto` carrying an exception message, so CHEFS receives a 200 with a failure body rather than an error.
4. **Store the field mapping** for the submission's `formVersionId`, if present.
5. Hand off to `IntakeFormSubmissionManager.ProcessFormSubmissionAsync`.

## Building the application

`IntakeFormSubmissionManager.ProcessFormSubmissionAsync` runs the whole creation inside **one transactional unit of work** — the comment in the source explains why: sequence-number generation for the Unity Application ID needs atomicity.

### Field mapping

`IIntakeFormSubmissionMapper.MapFormSubmissionFields(applicationForm, formSubmission, formVersionSubmissionHeaderMapping)` produces an `IntakeMapping` — a flat intermediate shape. The mapping used is the `SubmissionHeaderMapping` stored on the matching `ApplicationFormVersion`, looked up by the submission's `formVersionId`; a version with no stored mapping yields a null map and the mapper falls back to its defaults.

Values are then coerced defensively through `MappingUtil` when the `Application` is built — `ResolveAndTruncateField(255, …)` for the project name, `ConvertToDecimalFromStringDefaultZero`, `ConvertToIntFromString`, `ConvertDateFromChefsFormat`, `ConvertDateTimeFromStringDefaultNow`. Bad or missing values become defaults rather than failures.

### The Unity Application ID

Separate from `ReferenceNo` (always the CHEFS confirmation id), an optional human-readable id is generated from the form's configuration:

```text
Prefix == null or SuffixType == null      → no Unity ID
SuffixType == SequentialNumber            → {Prefix}{n:D5}   via ISequenceRepository, per prefix
                                             digits auto-expand past 99999
SuffixType == SubmissionNumber            → {Prefix}{ReferenceNo}
```

Generation is wrapped in **graceful degradation** at two levels: a failure inside `GenerateUnityApplicationIdAsync` is logged and returns null, and a failure escaping it is caught again in `CreateNewApplicationAsync` with the comment *"Application creation is priority"*. An application is never lost because its id could not be generated.

### Applicant resolution

Once the `Application` row exists:

```csharp
var applicant = await applicantService.CreateOrRetrieveApplicantAsync(intakeMap, application.Id);
application.ApplicantId = applicant.Id;                       // second write

await applicantService.CreateApplicantAgentAsync(applicantAgentDto);

try   { await applicantService.RelateDefaultSupplierAsync(applicantAgentDto); }
catch (Exception ex) { /* logged — SSL certificate errors from CAS */ }
```

The supplier call is deliberately swallowed; the source comment records that it was failing with SSL certificate errors and must not block intake. See [core-applicants.md](core-applicants.md) for how an applicant is matched or created, and [`payments/payments-suppliers-and-sites.md`](../payments/payments-suppliers-and-sites.md) for the supplier side.

### What else is written

| Step | Result |
|---|---|
| `SaveChefsFiles(formSubmission, application.Id)` | `ApplicationChefsFileAttachment` rows for each file in the submission |
| `ApplicationFormSubmission` insert | The raw `submission` node as JSON, the CHEFS submission GUID, the applicant, and `OidcSub` extracted by `IntakeSubmissionHelper.ExtractOidcSub` |
| `CustomFieldsIntakeSubmissionMapper.MapAndPersistCustomFields` | Flex worksheet instance values — see [`flex/flex-integration.md`](../flex/flex-integration.md) |
| Back-fill on the submission | `ApplicationFormVersionId` and `FormVersionId` set once the local form version is resolved |

The application is created directly in **`SUBMITTED`** — `ApplicationStatusId` is looked up from the `ApplicationStatus` table, not assigned from the enum.

## The events intake publishes

Two local events, in this order, before the unit of work is saved:

### `ApplicationProcessEvent`

The extension point. Its declared purpose in the source: *"Extend any further processing of the application here through local event bus and handlers."* It carries the `Application`, the `ApplicationFormVersion`, the `ApplicationFormSubmission`, and the **raw** submission.

| Handler | Module | Does |
|---|---|---|
| `DetermineElectoralDistrictHandler` | core | Resolves the electoral district from the applicant's physical or mailing address, per the form's `ElectoralDistrictAddressType` |
| `UpdateApplicantProfileCacheHandler` | core | Refreshes the applicant-profile cache |
| `GenerateReportDataHandler` | core | Generates reporting data for the new application |
| `QueueApplicationAIPipelineOnProcessHandler` | Unity.AI | Queues attachment summary, analysis and scoring — see [`ai/ai-generation-pipeline.md`](../ai/ai-generation-pipeline.md) |

### `ApplicationChangedEvent`

`{ Action = GrantApplicationAction.Submit, ApplicationId }`. Consumed by the Notifications module's `ScheduledNotificationEventHandler` to fire any configured event-driven email — see [`notifications/notifications-scheduled-notifications.md`](../notifications/notifications-scheduled-notifications.md).

Both are **local** bus events published inside the transaction, so a handler that throws can roll back the intake.

## The nightly resync

CHEFS webhooks can be missed. `IntakeSyncWorker` is a `[DisallowConcurrentExecution]` Quartz worker that reconciles what CHEFS holds against what Unity holds.

| Setting | Default | Meaning |
|---|---|---|
| `GrantManager.BackgroundJobs.IntakeResync_Expression` | `0 0 7,19 1/1 * ? *` | Twice daily — 07:00 and 19:00 UTC, i.e. 11 PM and 11 AM Pacific |
| `GrantManager.BackgroundJobs.IntakeResync_NumDaysToCheck` | `-4` | How far back to look |

For each tenant it calls `IApplicationFormSycnronizationService.GetMissingSubmissions(numberDaysBack)` (note the misspelled type name) and collects a per-tenant report. If anything is missing **and** `ASPNETCORE_ENVIRONMENT` is exactly `Production`, it emails a single summary to `grantmanagementsupport@gov.bc.ca` from `NoReply@gov.bc.ca`.

Two things follow from that: outside production the worker detects missing submissions and tells nobody, and the recipient address, the sender and the subject are hard-coded in the worker rather than configured.

The worker **reports**; it does not import. Recovering a missed submission is a separate action — `ApplicationIntakeAdminService` and the pull-side `SubmissionAppService` support re-fetching one on demand.

## Attachment resync

`IntakeFormSubmissionManager.ResyncSubmissionAttachments(applicationId)` re-pulls a submission's files from CHEFS. It validates carefully before touching anything, and each failure message ends with the same reassurance — *"existing attachments are unchanged"*:

- the submission record must exist,
- the form's `ChefsApplicationFormGuid` and the record's `ChefsSubmissionGuid` must both parse as GUIDs,
- CHEFS must return data,
- that data must have the expected shape (`submission.submission.data` and `version` both present).

Only then does it open a transactional unit of work and hand off to the mapper.

## Related pull-side services

| Service | Role |
|---|---|
| `SubmissionAppService` | Reads a submission from CHEFS on demand; lists submissions per form across tenants for admin screens; serves CHEFS file attachments |
| `ChefsAttachmentDownloadService` | Downloads attachment content from CHEFS |
| `ApplicantLookupService` | Resolves an applicant by Unity applicant id or BCeID business name, optionally creating one — used by the applicant portal |
| `ApplicationIntakeAdminService` | Administrative intake operations |
| `ChefsEventSubscriptionService` | Manages the webhook subscription registered with CHEFS |
