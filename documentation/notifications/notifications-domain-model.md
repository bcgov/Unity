# Notifications Domain Model

All entities live in `modules/Unity.Notifications/src/Unity.Notifications.Domain/`, all enums and string constants in `Unity.Notifications.Domain.Shared/`, and all EF Core mapping in `Unity.Notifications.EntityFrameworkCore/EntityFrameworkCore/NotificationsDbContextModelCreatingExtensions.cs`.

Every entity implements `IMultiTenant` and carries a nullable `TenantId`. ABP's automatic multi-tenancy filter does the isolation — no query in this module filters `TenantId` by hand, except where a host-side caller deliberately steps outside the ambient tenant (`ICurrentTenant.Change`).

## Persistence: two DbContexts, one of which is the real one

`NotificationsEntityFrameworkCoreModule` registers a `NotificationsDbContext` (connection string name `Tenant`, per `NotificationsDbProperties.ConnectionStringName`) whose `OnModelCreating` calls `ConfigureNotifications()`. But at runtime the notification tables are created and queried through the **host's** `GrantTenantDbContext`, which calls the same `modelBuilder.ConfigureNotifications()` extension from its own `OnModelCreating` (`Unity.GrantManager.EntityFrameworkCore/EntityFrameworkCore/GrantTenantDbContext.cs`). Tenant migrations therefore carry the notification tables, and the module's own DbContext is effectively a scaffold that keeps the module self-describing.

`NotificationsDbProperties` sets `DbSchema = "Notifications"` and an empty `DbTablePrefix`, so tables are `Notifications."EmailLogs"`, `Notifications."NotificationLogs"`, and so on.

There is a **second, unused** `GrantManagerDbContext` inside `Unity.Notifications.EntityFrameworkCore` holding a `DynamicUrl` DbSet — dead code, see [notifications-roadmap.md](notifications-roadmap.md#a-duplicate-dynamicurl-stack-that-nothing-registers).

## Email entities

### `EmailLog` (`Emails/EmailLog.cs`)

`AuditedAggregateRoot<Guid>`. The central record — one row per outbound message.

|Group|Fields|Notes|
|---|---|---|
|Ownership|`ApplicationId`, `ApplicantId`, `AssessmentId`, `ScheduledNotificationId?`|Plain Guids, not navigation properties. `ScheduledNotificationId` links back to the host `ScheduledNotification` that produced the message.|
|Addressing|`FromAddress`, `ToAddress`, `CC`, `BCC`|Comma-joined strings, not collections. Parsed with `ParseEmailList()` (`Unity.Modules.Shared.Utils`) on the way out.|
|Content|`Subject`, `Body`, `BodyType`, `Priority`, `Tag`, `TemplateName`|`BodyType` is `"html"` or `"text"`. `TemplateName` is stored but deliberately **not** sent to CHES — it is not part of the CHES `MessageObject` schema.|
|State|`Status`, `EmailType?`, `Recipient?`|`Status` is a plain string (see below). The two enums are persisted `HasConversion<string>()` with `MaxLength(32)`.|
|Scheduling|`SendOnDateTime?`, `SentDateTime?`|`SendOnDateTime` is the requested UTC send time; a future value flips status to `Scheduled` and type to `Delayed`. `SentDateTime` is set from the CHES response.|
|CHES|`ChesMsgId?`, `ChesResponse`, `ChesStatus`, `ChesHttpStatusCode?`, `RetryAttempts`|`ChesResponse` holds a serialized `{StatusCode, Headers, Body}` blob. `ChesMsgId` is extracted from `messages[0].msgId` and is what status/cancel calls key off.|
|Payments|`PaymentRequestIds`|Comma-joined Guids. Non-empty means a successful send publishes `FsbEmailSentEto` back to `Unity.Payments`.|

`EmailLog` re-declares `Id` with `[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, shadowing the base `AggregateRoot<Guid>.Id`.

### `EmailStatus` (`Emails/EmailStatus.cs`)

A **static class of string constants**, not an enum, and stored as free text with no length constraint or check:

`Created` · `Sent` · `Failed` · `Draft` · `Initialized` · `Cancelled` · `Scheduled`

`Created` is declared but never assigned anywhere in the codebase. The lifecycle actually used:

```text
                    ┌──────────────────────────────────────┐
  InitializeDraft   │                                      │
  ───────────────►  Draft ──(SendCustom)──►  Initialized ──┼──► Sent
                                             or Scheduled  │      (CHES 2xx)
                                                  │        │
                                                  │        └──► Failed
                                                  │             (CHES error, or retries exhausted,
                                                  │              or missing attachments)
                                                  └──────► Cancelled
                                                           (user cancels a non-Sent email)
```

### `EmailLogAttachment` (`Emails/EmailLogAttachment.cs`)

Metadata for one S3 object. `EmailLogId` **or** `TemplateId` is set (an attachment belongs to a draft/sent email, or to a reusable template). `OriginTemplateId` records that this row was copied from a template — which is what makes template re-application replaceable. `S3ObjectKey`, `FileName`, `DisplayName` (max 1024), `ContentType`, `FileSize`, `Time`, `UserId`.

Mapping: cascade-delete FKs to both `EmailLog` and `EmailTemplate` (both optional), plus indexes on `TemplateId`, `EmailLogId`, and `S3ObjectKey` (the last supports the shared-object reference check in `DeleteAttachmentAsync`).

### `EmailAddressConfiguration` (`EmailAddresses/`)

An allowlisted address with `EmailType` (one of `Sender`, `ReplyTo`, `NoReply`, `Inbound`, `Support`, `Other`, enforced in `EmailAddressConfigurationsAppService`), `Description`, `IsActive`, `IsDefault`. Two indexes matter:

- `(TenantId, EmailAddress, EmailType)` unique — no duplicate address/type pairs per tenant.
- `TenantId` unique **with filter `"IsDefault" = true`** — a Postgres partial index guaranteeing at most one default address per tenant at the database level, not just in service code.

The active default `Sender` row is the From address fallback used by `EmailNotificationManager.GetEmailObjectAsync` and `EmailNotificationService.SendCommentNotification`; if none exists both fall back to the literal `NoReply@gov.bc.ca`.

## Template entities (`Templates/`)

|Entity|Purpose|
|---|---|
|`EmailTemplate`|`FullAuditedAggregateRoot<Guid>` — `Name`, `Description`, `Subject`, `BodyText`, `BodyHTML`, `SendFrom`, `RecipientCategory?`, `RecipientIdentifier?`. The only soft-deleted entity in the module. Can own `EmailLogAttachment` rows.|
|`TemplateVariable`|`Name` (display), `Token` (the `{{token}}` placeholder), `MapTo` (dotted path on the application object). Seeded, not user-created.|
|`Trigger`, `TriggerSubscription`, `Subscriber`, `SubscriptionGroup`, `SubscriptionGroupSubscription`|A subscription/trigger model with full EF mapping and navigation properties — mapped, migrated, and **unreferenced by any service**. See [notifications-roadmap.md](notifications-roadmap.md#the-subscriptiontrigger-model-is-mapped-but-unused).|

`TemplateService` (declared in `Templates/TemplatesService.cs`, implementing `ITemplateService` from `Templates/ITemplatesService.cs` — note both class and interface are singular while the files are plural) is the only service touching templates: CRUD plus `GetTemplatesByTenant`, `GetTemplateByName`, `GetTemplateVariables`.

## Email group entities

`EmailGroup` (`Name`, `Description`, `Type`) and `EmailGroupUser` (`GroupId` → `EmailGroup`, `UserId` → an ABP identity user). Two groups are seeded per tenant by `NotificationsDataSeedContributor`:

- **`FSB-AP`** — recipients for PO-related payment notifications sent to Financial Services Branch.
- **`Payments`** — recipients for payment failure/error notifications.

`EmailGroupsAppService.DeleteAsync` throws `BusinessException("Unity.Notifications:EmailGroupInUse")` if the group's *name* appears in the `RecipientIdentifier` of any active `ScheduledNotification` with `RecipientCategory == "Internal"`. The link between a scheduled notification and a group is by name, not by id — renaming a group silently breaks that link.

## Realtime entities

### `NotificationLog` (`Logs/NotificationLog.cs`)

The durable record behind the Notification Logs page. Beyond the identity fields (`UserId?`, `SenderUserId?`, `SenderDisplayName?`) it carries three enums, all persisted as strings:

|Enum|Values|
|---|---|
|`NotificationLogType`|`SignalRSystemNotification`, `SignalRDirectMessage`, `SignalRGroupNotification`, `DbException`, `AbpHandledException`, `MiddlewareUnhandledException`, `PrometheusErrorCounterEvent`, `PrometheusExceptionCounterEvent`, `LegacyBridgeEvent`, `UnityException`|
|`NotificationLogChannel`|`SignalR`, `ExceptionPipeline`, `Prometheus`, `System`|
|`NotificationLogSeverity`|`Info`, `Warning`, `Error`, `Critical`|

Only `SignalRDirectMessage` / `SignalR` / `Info` are ever written today — every other value is declared for a pipeline that does not exist yet ([roadmap](notifications-roadmap.md#log-types-and-channels-declared-for-pipelines-that-dont-exist)).

Plus `Title` (required, 256), `Message` (required, unbounded), `Source` (required, 200), `SourceReference?`, `PayloadJson?` (Postgres **`jsonb`**), `CorrelationId?` (128), `IsDeliveredRealtime`, `DeliveryTarget?` (the SignalR group name), and diagnostics `ExceptionType?`/`ExceptionMessage?`/`StackExcerpt?`/`CommitSha?`/`Environment?`.

Indexes: `(TenantId, CreationTime)`, `(NotificationType, CreationTime)`, `CorrelationId`.

### `NotificationReadState` + `NotificationReadStateManager` (`ReadStates/`)

One row per `(TenantId, UserId)` — unique-indexed — holding `LastReadAt`. `NotificationReadStateManager` is the module's only domain service:

- `GetLastReadAtAsync` returns `DateTime.MinValue` when no row exists, so a brand-new user sees all history as unread.
- `MarkReadAsync` inserts or updates with `Clock.Now`, and **swallows `AbpDbConcurrencyException`** — two tabs advancing the same watermark concurrently is expected, and losing one write is harmless.

## Repositories

Custom repository interfaces live in `Unity.Notifications.Domain`, implementations in `Unity.Notifications.EntityFrameworkCore/Repositories/`.

|Interface|Beyond the generic base|
|---|---|
|`IEmailLogsRepository : IRepository<EmailLog, Guid>`|`GetByIdAsync`, `GetByApplicationIdAsync`, `GetByApplicationIdsAndStatusAsync`|
|`IEmailLogAttachmentRepository : IBasicRepository<…>`|`GetByEmailLogIdAsync`, `GetByTemplateIdAsync`, `GetOriginAttachmentsByEmailLogIdAsync`, `HasOtherReferencesAsync(s3ObjectKey, attachmentId)`|
|`ITemplatesRepository : IBasicRepository<…>`|`GetByIdAsync`, `GetByTenentIdAsync` *(sic)*, `GetByNameAsync`|
|`ITemplateVariablesRepository`, `IEmailGroupsRepository`, `IEmailGroupUsersRepository`, `IEmailAddressConfigurationsRepository`, `INotificationLogsRepository`, `INotificationReadStateRepository`|Marker interfaces over `IRepository<T, Guid>` with no extra members|

## Permissions

Declared in `Unity.Notifications.Application.Contracts/Permissions/NotificationsPermissions.cs`, registered by `NotificationsPermissionDefinitionProvider` under the group `Notifications`.

|Constant|String|Enforced at|
|---|---|---|
|`Email.Default`|`Notifications.Email`|Parent node — not itself an `[Authorize]` target|
|`Email.Send`|`Notifications.Email.Send`|`EmailNotificationService.CancelEmail`, the whole `EmailLogAttachmentAppService`|
|`Email.SendBulk`|`Notifications.Email.SendBulk`|Host bulk-email surfaces|
|`Email.DeleteDraft`|`Notifications.Email.DeleteDraft`|UI gate on the delete action|
|`Email.CancelScheduled`|`Notifications.Email.CancelScheduled`|UI gate on the cancel action|
|`Email.Schedule`|`Notifications.Email.Schedule`|UI gate on the send-later control|
|`Email.NotificationsTab`|`Notifications.Form.Tab`|Parent node for the form-level notification config tab|
|`Email.ScheduleCreate`|`Notifications.Form.Email.Schedule.Create`|Creating a form scheduled notification|
|`Email.ScheduleCancel`|`Notifications.Form.Email.Schedule.Cancel`|Cancelling a form scheduled notification|
|`NotificationList.Default` / `.View`|`Notifications.NotificationList` / `.View`|The Notification List page, `NotificationListAppService`, and the host `NotificationsController.EmailModal`|
|`Settings`|`SettingManagement.Notifications`|Added to ABP's **`SettingManagement`** group, not the Notifications group. Gates `EmailNotificationService.UpdateSettings` and the settings page contributor.|

Two mismatches worth knowing: the localization file defines `Permission:Notifications.Form.Email.Schedule.Delete`, but no such constant is declared or registered; and `Email.ScheduleCancel` is declared and localized but was not found on any `[Authorize]` attribute.

Everything on the realtime side is gated by the **role** `IdentityConsts.ITOperationsPermissionName` instead (`modules/Unity.SharedKernel/Permissions/IdentityConsts.cs`).

## Settings

Defined in `Domain/Settings/NotificationsSettingDefinitionProvider.cs`, keys in `NotificationsSettings`, all `isVisibleToClients: true`, `isInherited: false`, `isEncrypted: false`:

|Key|Default|Meaning|
|---|---|---|
|`GrantManager.Notifications.Mailing.DefaultFromAddress`|`NoReply.Unity@gov.bc.ca`|Seeded into `EmailAddressConfiguration` as the default `Sender` row; also read directly by the scheduled-notification handlers and the email widget.|
|`GrantManager.Notifications.Mailing.EmailMaxRetryAttempts`|`3`|Surfaced and edited on the settings tab. **Not read by the send pipeline** — `EmailConsumer` uses its own hard-coded `maxRetries = 10` ([roadmap](notifications-roadmap.md#the-configured-retry-limit-is-not-the-one-that-applies)).|
|`GrantManager.Notifications.Mailing.EnableEmailDelay`|`false`|Enables the "schedule this email" control on the per-application email widget.|

`NotificationsSettingsDataSeedContributor` explicitly writes `EnableEmailDelay = "false"` into each tenant's settings row when no explicit value exists, so existing tenants get the default on deploy rather than relying on the runtime fallback.

## Data seeding

`NotificationsDataSeedContributor` runs **per tenant only** (returns immediately when `context.TenantId == null`) and seeds three things:

1. **23 `TemplateVariable` rows** — the `{{token}}` → `MapTo` mapping used by scheduled notifications. Includes one repair path: an existing `category` variable whose `MapTo` is still the bare `category` is rewritten to `applicationForm.category`.
2. **The `FSB-AP` and `Payments` email groups**, if absent by name.
3. **The default sender address** — inserts an `EmailAddressConfiguration` for the `DefaultFromAddress` setting value, or promotes an existing matching row to `IsDefault` if no default exists yet.

Failures in the first two blocks are wrapped and rethrown as `InvalidOperationException`, aborting the seed.
