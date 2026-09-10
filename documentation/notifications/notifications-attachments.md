# Email Attachments

Attachment **bytes** live in S3; attachment **metadata** lives in the `Notifications."EmailLogAttachments"` table. Everything in this file is `EmailAttachmentService` (`Unity.Notifications.Application/Emails/EmailAttachmentService.cs`, `ITransientDependency`), except the HTTP-facing wrapper `EmailLogAttachmentAppService`.

The S3 client is an `IAmazonS3` singleton registered in `NotificationsApplicationModule` from `S3:Endpoint`, `S3:AccessKeyId`, `S3:SecretAccessKey`, with `RegionEndpoint = null` and `ForcePathStyle = true` — a path-style, non-AWS-region object store.

## Object key layout

Two key shapes, from two different upload methods:

```text
Email/Attachments/{tenantId|host}/{emailLogId}/{attachmentGuid}/{urlEncodedFileName}   ← UploadUserAttachmentAsync
Email/FSB-AP-Payments/{tenantId|host}/{emailLogId}/{urlEncodedFileName}                ← UploadAttachmentAsync
```

The user path interposes a fresh `Guid` per upload, so two files with the same name on the same email never collide and each copy of a template attachment gets its own object. The FSB path does not — it is only ever reached with generated, unique spreadsheet names.

`Uri.EscapeDataString(fileName)` is applied to the last segment in both, so the raw filename never reaches the key.

## Two upload paths, deliberately different on missing users

|Method|Caller|`UserId` when `ICurrentUser.Id` is null|
|---|---|---|
|`UploadAttachmentAsync`|`EmailNotificationHandler` — a local event handler that runs for schedule- and system-triggered emails|`Guid.Empty`, intentionally. There genuinely may be no interactive user, and the caller already wraps this in a try/catch that sends the email without the attachment on failure.|
|`UploadUserAttachmentAsync`|`EmailLogAttachmentAppService.UploadAsync`, reached from `AttachmentController` after validation|Throws `AbpAuthorizationException`. Attributing a user-uploaded file to `Guid.Empty` would look like a valid, specific user rather than an error.|

The comments in the source spell this out; it is a considered asymmetry, not an oversight.

## Template attachments: copy vs. replace

An `EmailTemplate` can own attachments (`EmailLogAttachment.TemplateId` set, `EmailLogId` null). Applying a template to an email copies those objects — it never shares them, so editing the template later cannot retroactively change an already-sent email.

### `CopyTemplateAttachmentsAsync(templateId, emailLogId, tenantId)`

Additive. Skips anything already copied, then copies the rest.

Deduplication is by **`(FileName, FileSize, ContentType)` among rows whose `OriginTemplateId == templateId`** — not by `S3ObjectKey`. Every copy gets a brand-new key (the `attachmentGuid` segment above), so a key-based check would never match and a re-run would duplicate every attachment.

### `ReplaceTemplateAttachmentsAsync(templateId, emailLogId, tenantId)`

Used when a composer swaps one template for another. The ordering is the point:

1. Validate that **every** source object exists (`ValidateAttachmentsExistAsync` against the template). This also catches orphaned template metadata when the same template is reapplied to an email that already has matching rows.
2. Copy all of them to fresh, email-owned S3 objects and insert the new rows.
3. **Only then** delete the previous template-origin attachments (`GetOriginAttachmentsByEmailLogIdAsync`).

Manually uploaded draft attachments have no `OriginTemplateId` and are left untouched.

### `CopyAttachmentsAsync` — the shared core

Copies with `CopyObjectRequest` within the same bucket, then bulk-inserts the metadata rows with `InsertManyAsync(autoSave: true)`. A missing source object is translated from `AmazonS3Exception` into `MissingEmailAttachmentsException`. On **any** failure it calls `DeleteCopiedObjectsBestEffortAsync` to remove the objects it already wrote, so a partial copy does not leak storage — and then rethrows.

## Validation: three checks, three races

`ValidateAttachmentsExistAsync` issues a `GetObjectMetadataAsync` (a HEAD, not a GET) per attachment, collects every missing filename, and throws a single `MissingEmailAttachmentsException` listing all of them.

`IsMissingObject` treats `404`, error code `NoSuchKey`, and error code `NotFound` as "missing" — different S3-compatible stores report this differently.

The same validation runs at three points, each closing a different window:

|Where|Why|
|---|---|
|`EmailAppService.SendAsync` (host, HTTP boundary)|So ABP serializes the user-friendly message straight back to the email composer, before an event is even published.|
|`EmailNotificationHandler` — twice, before mutating and before committing|So a missing object rolls the transaction back and the draft stays editable.|
|`EmailNotificationManager.QueueEmailAsync`|Last check before the message leaves for RabbitMQ.|
|`BuildEmailObjectWithAttachmentsAsync`, at send time|The final defense: `DownloadAttachmentFromS3Async` also converts a missing object into `MissingEmailAttachmentsException`, which `EmailConsumer` classifies as **non-retryable**.|

## `MissingEmailAttachmentsException`

A `UserFriendlyException` (so ABP surfaces its message verbatim) carrying the missing `FileNames` and an `EmailAttachmentValidationContext` (`Email` or `Template`) that selects the wording:

- **Template**: *"This template contains attachments that cannot be found: … Re-upload them in the email template before using it."*
- **Email**: *"One or more email attachments cannot be found: … Remove and re-upload them before sending."*

Filenames are normalized on construction: blanks become `"Unnamed attachment"`, then distinct case-insensitively and sorted, so the message is stable and free of duplicates.

## Deletion is reference-aware

`DeleteAttachmentAsync(attachment)`:

1. Ask `HasOtherReferencesAsync(attachment.S3ObjectKey, attachment.Id)` whether any **other** metadata row points at the same object.
2. Delete the metadata row. An `EntityNotFoundException` here returns quietly — already gone is success.
3. If other rows still reference the object, log and stop. The object stays.
4. Otherwise delete the S3 object — and if *that* fails, **log the error and do not rethrow**.

The comment in the source states the invariant: *"The database must never retain a key merely because best-effort storage cleanup failed. A leaked object is safer than deleting an object that another attachment still needs."* Metadata is the source of truth; orphaned bytes are an acceptable cost.

`DeleteOriginAttachmentsAsync(emailLogId)` deletes all template-origin attachments on an email and returns the count. `EmailNotificationManager.DeleteEmailLogAsync` deletes every attachment on an email before deleting the log row.

## The HTTP surface

`EmailLogAttachmentAppService` — `[Authorize(NotificationsPermissions.Email.Send)]`, exposing both `IEmailLogAttachmentAppService` and `IEmailLogAttachmentUploadService`.

|Method|Notes|
|---|---|
|`GetListByEmailLogIdAsync` / `GetListByTemplateIdAsync`|Return `EmailLogAttachmentDto`s with `AttachedBy` resolved through `IExternalUserLookupServiceProvider` (falling back to an empty string on lookup failure).|
|`DeleteAsync(id)`|Idempotent — a missing row returns success. Template attachments delete freely; email attachments delete **only from `Draft` emails**, otherwise `UserFriendlyException("Attachments can only be deleted from draft emails.")`.|
|`GetTotalFileSizeByEmailLogIdAsync(emailLogId?, templateId?)`|Sums `FileSize` from metadata, not from S3 — a cheap in-database check. Backs the composer's total-size indicator and the bulk-send size gate.|
|`UploadAsync(...)`|**`[RemoteService(false)]`** — deliberately not exposed over HTTP.|

That last one is the important gate. `UploadAsync` takes a raw `fileName`/`content`/`contentType` with none of the allowlist, size, or content-type checks that `AttachmentController` enforces. It must only be reached in-process through `IEmailLogAttachmentUploadService`, from a caller that has already run those checks. Exposing it would let an HTTP client bypass validation entirely while still holding a legitimate `Email.Send` permission — the permission alone is not the control here.

## Size limits

Limits are configuration, read where they are enforced, not centralized in this service:

|Config key|Enforced by|
|---|---|
|`S3:AllowedFileTypes`|`AttachmentController`, surfaced on the settings tab|
|`S3:MaxFileSize`|`AttachmentController`|
|`S3:EmailAttachmentMaxFileSize`|Per-file limit for email attachments|
|`S3:EmailAttachmentsTotalMaxFileSize`|Per-email total, default `25` (MB)|

`BulkEmailNotificationAppService` re-checks the total independently before sending each draft, using the same `TryParse`-with-fallback-to-25 pattern, because "neither the per-upload size gate nor the modal's own UI check can catch every path an attachment can arrive by" — copying a template's attachments onto a draft applies no size check at all.

## Tests

`test/Unity.Notifications.Application.Tests/EmailAttachmentServiceTests.cs` is the module's only substantial test file. It is the reference for how the copy/replace/validate behaviours are expected to work; the rest of the module has test *scaffolding* (`NotificationsTestBase`, module classes, a fake principal accessor) but no tests.
