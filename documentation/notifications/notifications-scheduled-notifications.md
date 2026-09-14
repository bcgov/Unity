# Scheduled Notifications

"Scheduled notification" here means **a configuration**, not a queued message: *when applications on form X reach status Y (or when date field Z passes), render template T and send it to recipients R*. Program staff configure these per form; the system fires them automatically.

Everything on this page lives in the **host**, not in `Unity.Notifications` — the module knows how to send, not when. The pieces:

| File | Role |
|---|---|
| `Unity.GrantManager.Domain/Notifications/ScheduledNotification.cs` | The configuration entity |
| `Unity.GrantManager.Domain/Notifications/ApplicationScheduledNotificationTracking.cs` | The already-sent ledger (class `ScheduledNotificationTracking`) |
| `Unity.GrantManager.Application/Events/ScheduledNotificationEventHandler.cs` | Event-driven trigger |
| `Unity.GrantManager.Application/Events/DateBasedScheduledNotificationJob.cs` | Date-driven trigger (nightly Quartz job) |
| `Unity.GrantManager.Application/Events/ScheduledNotificationHelper.cs` | Shared token rendering + recipient resolution |
| `Unity.GrantManager.Web/Controllers/FormNotificationsApiController.cs` | The form-level configuration API |

Both triggers converge on the same output: an `EmailNotificationEvent` published to the local bus, which then travels the normal pipeline ([notifications-email-pipeline.md](notifications-email-pipeline.md)).

## The `ScheduledNotification` entity

`FullAuditedAggregateRoot<Guid>`, tenant-scoped.

|Field|Meaning|
|---|---|
|`FormId`|The application form this configuration applies to|
|`EmailTemplateId`|The `EmailTemplate` to render|
|`TriggerType`|`"Date"` or `"Event"` — a plain string|
|`IsActive`|Soft on/off switch, checked by both triggers|
|`Module`|`null`/`"Application"` for application-status events, `"Payment"` for payment-status events|
|`ApplicationStatusId`, `ApplicationStatus`|Event trigger: the status the application must reach|
|`EventType`|Payment-module event trigger: the payment status name|
|`DateField`|Date trigger: `"DueDate"`, `"NotificationDate"`, `"ContractNotificationDate"`, etc.|
|`TriggerDetail`|Free-text detail carried for the UI|
|`RecipientCategory`|`"Internal"` or `"External"` — decides how `RecipientIdentifier` is interpreted|
|`RecipientIdentifier`|Comma-separated. For `Internal`: email **group names**. For `External`: `ApplicationContact` and/or `SigningAuthority`|

`RecipientCategory` is doing double duty: it selects the recipient resolution strategy *and*, once the email exists, it is what `EmailNotificationHandler.StampClassificationAsync` reads back to set `EmailLog.Recipient`.

## Trigger 1 — event-driven

`ScheduledNotificationEventHandler` implements `ILocalEventHandler<ApplicationChangedEvent>` **and** `ILocalEventHandler<PaymentStatusChangedEvent>`.

Both handlers follow the same shape:

1. Return immediately unless the `Unity.Notifications` feature is enabled.
2. Load the application with `includeDetails: true` — the navigation properties (`Applicant`, `ApplicationStatus`, `ApplicationForm`) are needed for token substitution.
3. Query active matching configurations:
   - **Application status**: `FormId == application.ApplicationFormId && TriggerType == "Event" && IsActive && (Module == null || Module == "Application") && ApplicationStatusId == application.ApplicationStatusId`
   - **Payment status**: same, but `Module == "Payment" && EventType == eventData.Status.ToString()`
4. Read the tenant's `DefaultFromAddress` setting once for the batch (falling back to `NoReply@gov.bc.ca`).
5. Load the `ApplicantAgent` once for the batch — used for both contact tokens and recipient resolution.
6. Call `ProcessNotificationAsync` per configuration.

The whole body is wrapped in a `try`/`catch` that logs and swallows. A notification failure never fails the status change that triggered it.

### `ProcessNotificationAsync`

Skips (with a warning) when `RecipientCategory` or `RecipientIdentifier` is blank, or when the referenced template no longer exists. Then:

- Builds token values, renders `template.Subject` and the body — **`BodyHTML` if non-blank, otherwise `BodyText`**.
- Constructs an `EmailNotificationEvent` with `Action = EmailAction.SendEventDriven`, carrying `ScheduledNotificationId`, `TemplateId`, `EmailTemplateName`, and the rendered subject/body.
- Dispatches on `RecipientCategory` to `PublishToEmailGroupAsync` (Internal) or `PublishToExternalRecipientAsync` (External). An unrecognized category logs a warning and sends nothing.

Note the event is constructed with `Action = SendEventDriven` but no `EmailAddressList` — the helper fills that in and publishes. If the helper resolves zero addresses, it returns without publishing, so no `EmailLog` row is ever created.

## Trigger 2 — date-driven

`DateBasedScheduledNotificationJob` is a `QuartzBackgroundWorkerBase` with `[DisallowConcurrentExecution]`.

### Schedule

The cron expression is read in the constructor from `SettingsConstants.BackgroundJobs.DateBasedNotificationSchedule_Expression`, validated with `CronExpression.IsValidExpression`, and falls back to `"0 0 2 * * ?"` on an invalid or unreadable value. Both the invalid-expression and read-failure paths log a warning and continue with the default rather than failing startup. The trigger uses `WithMisfireHandlingInstructionIgnoreMisfires()` — a missed run is skipped, not replayed in a burst.

Quartz auto-registration itself is gated by `BackgroundJobs:Quartz:IsAutoRegisterEnabled` in configuration (`NotificationsWebModule` configures the same options for its own module).

### Per-tenant execution

The job iterates tenants and runs `ProcessTenantNotificationsAsync` inside each tenant's context, feature-checking `Unity.Notifications` per tenant.

The body is written explicitly to avoid N+1 queries, and the comments in the source label each optimization. In order:

1. All active configurations with a non-empty `DateField`.
2. **One** application query across every relevant `FormId`, filtered to rows where any of `DueDate`, `ProjectStartDate`, `ProjectEndDate`, `NotificationDate`, `ContractExecutionDate` is non-null and `<= today` (UTC date).
3. **One** batch query for every `ScheduledNotificationTracking` row belonging to those configurations.
4. **One** batch load of all needed templates, keyed by id.
5. **One** batch load of all `ApplicantAgent` rows for those applications, keyed by application id.
6. Per configuration: take the applications for that form, subtract the ones already tracked for `(ScheduledNotificationId, DateField)`, and process the remainder.
7. Collect tracking records in memory and `InsertManyAsync` them **once** at the end.

Each application is then rendered and published exactly as the event path does, with `Action = EmailAction.SendDateDriven`.

### Duplicate suppression

`ScheduledNotificationTracking` is the ledger: `(ApplicationId, ScheduledNotificationId, DateField, NotificationSentDate)`. The job filters out any application that already has a tracking row for that notification **and** that date field, which is why the same application can be notified separately for its due date and its contract date.

Two consequences worth knowing:

- A tracking row is created when the event is **published**, not when the email is sent. If the pipeline later fails permanently, the ledger still says "notified" and the job will never retry it.
- Tracking rows are inserted in one batch at the end of the tenant's run. A crash mid-run means the whole tenant's batch is lost and those applications will be notified again on the next run.

## Token substitution

`ScheduledNotificationHelper.BuildTokenValues` builds a case-insensitive dictionary from the application and applicant agent; `RenderTemplate` replaces `{{token}}` occurrences using a compile-time `[GeneratedRegex(@"\{\{(\w+)\}\}")]`. **Unknown tokens are left in place** — a typo renders as literal `{{typo}}` in the delivered email rather than as a blank.

Navigation-property access is defensively wrapped: `application.Applicant`, `.ApplicationStatus`, and `.ApplicationForm` are each read in a `try`/`catch` that falls back to `null`, because the entity may have been loaded without details.

The tokens produced:

|Token|Source|
|---|---|
|`applicant_name`, `applicant_id`|`Applicant.ApplicantName`, `.UnityApplicantId`|
|`organization_name`|`Applicant.OrgName` ?? `.NonRegisteredBusinessName`|
|`submission_number`, `submission_date`|`ReferenceNo`, `SubmissionDate` (`yyyy-MM-dd`)|
|`status`|`ApplicationStatus.StatusCode`|
|`approved_amount`, `requested_amount`, `recommended_amount`|Formatted `$#,##0.00`|
|`approval_date`|`FinalDecisionDate` (`yyyy-MM-dd`)|
|`decline_rationale`, `community`|`DeclineRational`, `Community`|
|`project_name`, `project_summary`, `project_start_date`, `project_end_date`|Application project fields|
|`signing_authority_full_name`, `signing_authority_title`|Application signing-authority fields|
|`contact_full_name`, `contact_title`|`ApplicantAgent.Name`, `.Title`|
|`category`|`ApplicationForm.Category`|
|`today_date`|`DateTime.Today` as `MMMM d, yyyy`|
|`unity_application_id`|`UnityApplicationId`|

These must stay in sync with the `TemplateVariable` rows seeded by `NotificationsDataSeedContributor`, which is what the template editor offers the user. The seed's `MapTo` column is documentation of intent — the actual resolution is this hand-written dictionary, not reflection over `MapTo`. Adding a variable therefore takes **two** edits: a seed row and a dictionary entry.

The one seeded variable with no dictionary entry is `today_date`'s `MapTo` (an empty string) — deliberate, since it comes from the clock rather than the application.

## Recipient resolution

### Internal — `PublishToEmailGroupAsync` → `GetInternalRecipientEmailAddressesAsync`

`RecipientIdentifier` is a comma-separated list of **email group names**. For each name:

1. Find the `EmailGroup` by case-insensitive name (warn and skip if absent).
2. Load its `EmailGroupUser` rows (warn and skip if the group is empty).
3. For each member, resolve an email through `IIdentityUserIntegrationService.FindByIdAsync`; a per-user lookup failure is logged and skipped rather than failing the batch.

Addresses accumulate into a case-insensitive `HashSet`, are joined with `"; "`, then split and de-duplicated again by the caller. Zero resolvable addresses → log a warning and publish nothing.

The whole method is wrapped in a `try`/`catch` returning `string.Empty` on failure, so a directory outage degrades to "no recipients" rather than an exception.

Because the link is **by group name**, renaming an email group silently detaches it from every scheduled notification referencing it. `EmailGroupsAppService.DeleteAsync` guards deletion (`BusinessException("Unity.Notifications:EmailGroupInUse")`) but nothing guards renaming — see [notifications-roadmap.md](notifications-roadmap.md#email-groups-are-linked-to-scheduled-notifications-by-name).

### External — `PublishToExternalRecipientAsync`

`RecipientIdentifier` is a comma-separated list of two recognized literals:

|Identifier|Resolves to|
|---|---|
|`ApplicationContact`|`ApplicantAgent.Email`|
|`SigningAuthority`|`Application.SigningAuthorityEmail`|

Anything else logs a warning and is skipped. A recognized identifier with no address on this particular application is also logged and skipped. Zero resolvable addresses → nothing published.

## Configuration API

`FormNotificationsApiController` (`api/form-notifications`) is the CRUD and lookup surface behind the form-level Notifications tab:

|Route|Purpose|
|---|---|
|`GET /{formId}` · `POST /{formId}` · `PUT /{formId}/{id}` · `DELETE /{formId}/{id}`|Scheduled notification CRUD for a form|
|`GET /templates`|Templates available to attach|
|`GET /templates/{templateId}/resolved-recipients`|Preview of who a template would reach|
|`GET /statuses` · `GET /payment-statuses` · `GET /recipients`|Lookup lists for the configuration UI|
|`GET /can-delete-template/{templateId}` · `GET /template-notification-plans/{templateId}`|Guard a template deletion by showing what still references it|
|`POST /email-template/{templateId}/copy-attachments` · `GET /email-template/{templateId}/validate-attachments` · `DELETE /email-log/{emailLogId}/origin-attachments`|The template-attachment operations described in [notifications-attachments.md](notifications-attachments.md)|

Access is gated by the `Notifications.Form.Tab` permission family (`Notifications.Form.Email.Schedule.Create` / `.Cancel`).
