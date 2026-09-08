# The Email Pipeline

This is the core of the module. Every email — user-composed, bulk, scheduled, payment-driven — travels the same four stages. The only exception is `ExceptionLogAppService`'s alert mail, which calls `SendEmailNotification` directly and skips both the log row and the queue.

```text
 ┌─ 1. PUBLISH ─────────────────────────────────────────────────────────────────┐
 │  Any host caller builds an EmailNotificationEvent and publishes it on the     │
 │  ABP local event bus. EmailAction says what kind of send this is.             │
 └──────────────────────────────────────┬───────────────────────────────────────┘
                                        ▼
 ┌─ 2. MATERIALISE ─────────────────────────────────────────────────────────────┐
 │  EmailNotificationHandler (ILocalEventHandler<EmailNotificationEvent>)        │
 │  · feature check → tenant switch → NEW transactional unit of work             │
 │  · dispatch on Action → create or update an EmailLog                          │
 │  · upload / copy attachments to S3, validate every object exists              │
 │  · stamp RecipientType, commit, THEN queue                                    │
 └──────────────────────────────────────┬───────────────────────────────────────┘
                                        ▼
 ┌─ 3. QUEUE ───────────────────────────────────────────────────────────────────┐
 │  EmailQueueService wraps {Id, TenantId, RetryAttempts} in an EmailMessages    │
 │  envelope and publishes to RabbitMQ (TTL 10 min first pass, 20 min on retry). │
 └──────────────────────────────────────┬───────────────────────────────────────┘
                                        ▼
 ┌─ 4. SEND ────────────────────────────────────────────────────────────────────┐
 │  EmailConsumer re-enters the tenant, reloads the EmailLog, sends via CHES,    │
 │  writes back status / ChesResponse / ChesMsgId / SentDateTime, and either     │
 │  finishes or re-queues for retry.                                            │
 └──────────────────────────────────────────────────────────────────────────────┘
```

## Stage 1 — the event

`EmailNotificationEvent` (`Unity.Notifications.Application/Events/EmailNotificationEvent.cs`) is a plain class, not an ETO — it travels the **local** event bus, in-process, not the distributed bus. Key fields:

- `Id` — `Guid.Empty` means "create a new `EmailLog`"; a non-empty value means "update this existing draft".
- `TenantId` — the handler switches into this tenant before doing anything, so publishers running outside a request context (background jobs, the payments module iterating tenants) still write to the right database.
- `Action` — an `EmailAction`, the dispatch key.
- `EmailAddressList` / `Cc` / `Bcc` — collections here, joined to comma strings on the `EmailLog`.
- `TemplateId` — non-empty means "copy this template's attachments onto the email".
- `ScheduledNotificationId?` — set by the scheduled-notification paths; used to classify the recipient and to trace a message back to its configuration.
- `EmailAttachments` — inline `{FileName, Content, ContentType}` byte payloads, used by the FSB path which generates a spreadsheet in memory.
- `SendOnDateTime?` — a future UTC time turns this into a delayed send.
- `PaymentRequestIds` — carried through to the `EmailLog` so a successful send can notify `Unity.Payments`.

### Which actions are actually routed

`EmailNotificationEventAsync` handles four branches:

|`EmailAction`|Handler method|Behaviour|
|---|---|---|
|`SendCustom`, `SendEventDriven`, `SendDateDriven`|`HandleSendCustomEmail`|Create-or-update the log, apply the template's attachments, set `EmailType`, attach the scheduled-notification id, classify the recipient. **Returns the log → it gets queued.**|
|`SaveDraft`|`HandleSaveDraftAndReturnNull`|Same create-or-update with status `Draft`, then deliberately returns `null` so nothing is queued.|
|`SendFailedSummary`|`HandleFailedSummary`|Fixed subject `"CAS Payment Failure Notification"`, `RecipientType.Internal`.|
|`SendFsbNotification`|`HandleFsbNotification`|Subject from the event (fallback `"FSB Payment Notification"`), uploads the inline attachments, records `PaymentRequestIds`, `RecipientType.Internal`.|

`Retry`, `SendByTemplateId`, `SendApproval`, and `SendDecline` are declared on the enum but fall through the `switch` to `null` — publishing one of them silently does nothing.

Every branch first calls `HasRecipients`, which logs a warning and returns `null` when `EmailAddressList` is empty. Note that a *non-empty* list of unparseable addresses (`";"`, `","`) still passes this check and is only caught later — which is why `BulkEmailNotificationAppService` runs its own `ParseEmailList()` check before publishing.

## Stage 2 — `EmailNotificationHandler`

Lives at `Unity.Notifications.Application/Events/EmailNotificationHandler.cs`, in namespace `Unity.GrantManager.Events`.

```csharp
if (!await featureChecker.IsEnabledAsync("Unity.Notifications")) return;

using (currentTenant.Change(eventData.TenantId))
{
    using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

    if (eventData.Action == EmailAction.SendCustom && eventData.Id != Guid.Empty)
        await emailAttachmentService.ValidateEmailAttachmentsAsync(eventData.Id);   // fail before mutating

    var emailLog = await EmailNotificationEventAsync(eventData);

    if (emailLog != null)
        await emailAttachmentService.ValidateEmailAttachmentsAsync(emailLog.Id);    // fail before commit

    await uow.CompleteAsync();

    if (emailLog != null)
        await emailNotificationService.SendEmailToQueue(emailLog);                  // queue only after commit
}
```

Three deliberate orderings here, each worth preserving:

- **Validate twice.** The first check fails a send of an existing draft *before* any S3 object is copied or the draft is mutated. The second fails *before* the unit of work commits, so a missing object rolls back the transition out of `Draft` and the user can remove and re-upload the file.
- **Own transactional unit of work.** `requiresNew: true` means the email log and its attachment rows commit or roll back as one, independently of whatever transaction the publisher was in.
- **Queue after commit.** `SendEmailToQueue` is outside `uow.CompleteAsync()`, so the consumer can never dequeue a message whose `EmailLog` row is not yet visible.

### Recipient classification

`StampClassificationAsync` decides `RecipientType`:

- If the log has a `ScheduledNotificationId`, the linked `ScheduledNotification.RecipientCategory == "Internal"` (case-insensitive) makes it `Internal`, otherwise `External`.
- Otherwise an explicitly pre-set `Internal` (the payment/FSB paths) is preserved; everything else is `External`.

### Attachment failures are not uniform

`InitializeEmailAndUploadAttachments` catches every exception from S3 upload, logs it, and **continues** — an email whose attachment upload failed is still sent, without the attachment. The template-copy path is stricter: for an interactive `SendCustom` with no scheduled-notification id, a copy failure throws `UserFriendlyException("The template attachments could not be prepared. The email was not sent.")`, because the compose-and-send flow has no pre-existing draft rows to fall back on. Scheduled sends keep the lenient behaviour: log and carry on.

## Stage 3 — `EmailQueueService`

`Unity.Notifications.Application/Integrations/RabbitMQ/EmailQueueService.cs` wraps the event in an `EmailMessages` envelope (`ITenantedQueueMessage`) and publishes it through `IQueueProducer<EmailMessages>`.

|Method|TTL|Delay before publish|
|---|---|---|
|`SendToEmailEventQueueAsync`|10 minutes|none — first attempt|
|`SendToEmailDelayedQueueAsync`|20 minutes|`5 minutes × (RetryAttempts + 1)`, via an in-process `await Task.Delay`|

That `Task.Delay` is the retry backoff, and it holds the consumer's thread rather than using a RabbitMQ delayed-exchange or dead-letter TTL. On attempt 10 it parks for 55 minutes — well past the 20-minute message TTL. See [notifications-roadmap.md](notifications-roadmap.md#retry-backoff-is-an-in-process-taskdelay).

Both methods catch and log every exception rather than rethrowing, so a broker outage is silent to the caller: the `EmailLog` row exists and stays `Initialized` forever.

`EmailNotificationManager.QueueEmailAsync` validates the attachments one more time before publishing — the third of three checks, each guarding a different race.

## Stage 4 — `EmailConsumer`

`Unity.Notifications.Application/Integrations/RabbitMQ/EmailConsumer.cs`, registered by `NotificationsApplicationModule` via `context.Services.AddQueueMessageConsumer<EmailConsumer, EmailMessages>()`.

```csharp
ValidateMessage(evt);                          // throws if Id or TenantId is empty
using (currentTenant.Change(evt.TenantId))
{
    using var uow = unitOfWorkManager.Begin(requiresNew: true);
    var emailLog = await emailNotificationService.GetEmailLogById(evt.Id);

    if (emailLog == null || !ShouldProcessEmail(emailLog))  // already Sent or Cancelled
        return;                                             // idempotent: duplicate deliveries no-op

    await ProcessEmailAsync(emailLog, evt, uow);
    await uow.CompleteAsync();
}
```

### Failure classification

`ProcessEmailAsync` sorts failures into three buckets, which is the most consequential logic in the file:

|Failure|Status|Retried?|
|---|---|---|
|`RetryAttempts > 10`|`Failed`|No — give up|
|`MissingEmailAttachmentsException`|`Failed`, CHES fields cleared|**No** — a missing S3 object will still be missing next time|
|`AmazonS3Exception` (download failure)|`Failed`|Yes — transient storage problem|
|Any other exception during send|`Failed`|Yes|
|CHES returned 429, 500, 502, 503, 504|whatever `UpdateEmailLogStatus` set|Yes|
|CHES returned any other status|`Sent` (2xx) or `Failed`|No|

Note that `emailLog.Status` is set to `Failed` *before* a retry is scheduled, so a message mid-retry reads as `Failed` in the UI until an attempt succeeds.

### `UpdateEmailLogStatus`

- Serializes `{StatusCode, Headers, Body}` into `ChesResponse`; sets `ChesHttpStatusCode` (numeric string) and `ChesStatus` (the enum name).
- **Preserves `Scheduled`**: a scheduled email whose `SendOnDateTime` has not passed keeps status `Scheduled` regardless of the response — CHES has accepted it for later delivery, and flipping it to `Sent` would be a lie. Only once the send time has passed does the response decide `Sent` vs `Failed`.
- Extracts `messages[0].msgId` into `ChesMsgId` on success; a parse failure logs a warning rather than failing the send.
- Sets `SentDateTime` from the CHES `Date` response header, **sanitized**: `SanitizeResponseDateTime` rejects a header more than 5 minutes in the future or 15 minutes in the past and substitutes `DateTime.UtcNow`. This treats an external system's clock as untrusted input.
- If `PaymentRequestIds` is non-empty, publishes `FsbEmailSentEto { EmailLogId, PaymentRequestIds, SentDate, TenantId }` on the local event bus so `Unity.Payments` can mark those requests notified.

### Concurrency-safe saves

`SaveEmailLogWithRetryAsync` retries up to 3 times on `AbpDbConcurrencyException`/`DbUpdateConcurrencyException`, waiting 100 ms and copying the fresh row's `ConcurrencyStamp` onto the in-memory entity before retrying. This matters because a user can be editing or cancelling the same draft in the UI while the consumer is writing to it.

## The CHES integration

`Integrations/Ches/ChesClientService.cs` — `[IntegrationService]`, `[RemoteService(false, Name = "Ches")]`.

|Method|Call|
|---|---|
|`SendAsync(object emailRequest)`|`POST {NOTIFICATION_API_BASE}/email`|
|`GetStatusAsync(Guid messageId)`|`GET {NOTIFICATION_API_BASE}/status?msgId={id}`|
|`CancelEmailAsync(Guid messageId)`|`DELETE {NOTIFICATION_API_BASE}/cancel/{id}`|

Both base URLs come from `DynamicUrl` rows resolved through the host's `IEndpointManagementAppService` (`NOTIFICATION_API_BASE`, `NOTIFICATION_AUTH`) — they are configuration, not constants. The bearer token is fetched by `TokenService` using `ChesClientOptions.ChesClientId`/`ChesClientSecret` and cached in `IDistributedCache<TokenValidationResponse, string>`. Requests go through `IResilientHttpRequest`, which owns the serialization and resilience policy.

### Building the CHES message object

`EmailNotificationManager.GetEmailObjectAsync` builds an `ExpandoObject` matching the CHES `MessageObject` schema:

```json
{ "body", "bodyType", "encoding": "utf-8", "from", "priority": "normal",
  "subject", "tag": "tag", "to": [...],
  "cc": [...],        // only when provided — CHES wants arrays, never null
  "bcc": [...],       // ditto
  "delayTS": 1234567890123,   // only when SendOnDateTime is set
  "attachments": [ { "content": "<base64>", "contentType", "encoding": "base64", "filename" } ] }
```

Two subtleties:

- **`templateName` is excluded** when sending. It is not part of the CHES schema; it is stored on the `EmailLog` for Unity's own reporting and stripped via the `excludeTemplate` flag.
- **`delayTS` normalization.** `NormalizeToUtc` converts a `DateTimeKind.Local` value with `ToUniversalTime()`, passes `Utc` through, and for `Unspecified` — the kind that comes back from Postgres and from unparsed form input — applies a hard-coded `BcPermanentDstOffset = -7 hours`. That is BC's *permanent* daylight-time offset, which the province has legislated but not yet adopted; today PST is -8 in winter. See [notifications-roadmap.md](notifications-roadmap.md#the-hard-coded--7-timezone-offset).

## Scheduling and cancelling

A `SendOnDateTime` in the future makes `DetermineSendStatus` return `Scheduled` and `DetermineEmailType` return `Delayed`. CHES holds the message and Unity holds the `ChesMsgId`.

`DeleteEmailLogAsync` is the interesting path. Deleting a scheduled email that already has a `ChesMsgId` first calls `SyncScheduledEmailStatusAsync`, which asks CHES for the real state:

- `completed` or `accepted` → the message is already out. Mark the log `Sent`, persist, and throw `UserFriendlyException("This scheduled email has already been sent and cannot be deleted.")`.
- `pending` → still queued at CHES. Call `CancelEmailAsync` to withdraw it; a failed cancel throws a user-friendly error rather than deleting a row for a message that will still arrive.
- anything else → record the status and proceed with deletion.
- Any unexpected exception becomes `UserFriendlyException("Unable to verify the status of the scheduled email. Please try again.")` — refusing to delete blind is the safe default.

`ExtractChesStatus` handles CHES returning either an object or a single-element array.

Deletion then removes every S3 attachment before deleting the row, and tolerates `AbpDbConcurrencyException` by reloading and retrying once (or succeeding silently if the row is already gone).

`CancelEmailLogAsync` is the softer sibling: it refuses to cancel a `Sent` email and otherwise just sets `Status = Cancelled` — it does **not** call CHES, so cancelling a scheduled email through this path leaves the message queued at CHES to be delivered anyway.

## Two service layers

|Type|Role|
|---|---|
|`EmailNotificationManager` (`DomainService`, implements `IEmailNotificationManager`)|All the real behaviour: creating/updating/deleting logs, building the CHES payload, sending, queueing, CHES status sync.|
|`EmailNotificationService` (`ApplicationService`, implements `IEmailNotificationService`)|A thin pass-through over the manager, plus three things only it does: `SendCommentNotification`, `GetHistoryByApplicationId` (which enriches logs with resolved sender names), and `UpdateSettings`.|

`EmailNotificationService.SendCommentNotification` is the one place a template is rendered without going near `EmailTemplate`: it loads the **embedded resource** `Unity.Notifications.EmailTemplates.CommentNotification.cshtml`, does literal string replacement of `@Model.CurrentUserText`, `@Html.Raw(Model.CommentBody)`, `@Model.CommentLink` and `@model dynamic`, HTML-encodes the user text and link, and runs the comment body through `IMarkdownRenderer`. It builds a deep link to `/GrantApplications/Details` or `/GrantApplicants/Details` (by `CommentType`) from `App:SelfUrl`, and sends one email per mentioned address. This path is synchronous — no `EmailLog`, no queue.

`GetPendingEmailsCountAsync` (surfaced as `GetEmailsChesWithNoResponseCountAsync`, used by the health-check worker) counts logs that are `Sent` with no `ChesResponse`, or `Initialized` for more than 10 minutes — i.e. messages the pipeline appears to have dropped. It loads **every** `EmailLog` in the tenant and filters in memory ([roadmap](notifications-roadmap.md#getpendingemailscountasync-loads-every-email-log)).
