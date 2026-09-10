# Notifications Overview

## What problem it solves

Unity Portal has to talk to people outside the browser session. Two different kinds of talking, with almost nothing in common except the module they live in:

1. **Email.** Grant staff send messages to applicants (approval, decline, ad-hoc correspondence, bulk batches), and the system sends messages on its own initiative (payment failure summaries, FSB payment notifications with spreadsheet attachments, exception alerts, comment @-mentions, scheduled reminders driven by application dates or status changes). All of it goes out through **CHES**, the BC Government Common Hosted Email Service — Unity never speaks SMTP.
2. **In-app realtime.** IT Operations needs to see who is currently online across tenants, send a user or a whole tenant a message that appears immediately in their browser, and review a durable log of what was delivered. That runs over SignalR.

Both halves write to the same `Notifications` database schema, are enabled by the same tenant feature flag family, and ship in the same set of projects — but they share no code paths. Read them as two modules that happen to be co-located.

## The two big architectural facts

### 1. Every email is a database row before it is an email

Nothing calls CHES synchronously from a request thread. The pipeline is always:

```text
caller publishes EmailNotificationEvent (ABP local event bus)
        ↓
EmailNotificationHandler          creates/updates an EmailLog row, uploads or copies attachments
        ↓                          — all inside its own transactional unit of work
EmailQueueService                 publishes an EmailMessages envelope to RabbitMQ
        ↓
EmailConsumer                     re-enters the tenant context, sends via CHES, records the
                                   response/status/msgId back onto the same EmailLog row
```

The `EmailLog` row is the unit of work, the audit record, the retry token, and the thing the UI lists. Its `Status` (`Draft` → `Initialized`/`Scheduled` → `Sent`/`Failed`/`Cancelled`) is the state machine everything else keys off. See [notifications-email-pipeline.md](notifications-email-pipeline.md).

### 2. The module defines the plumbing; the host defines *when* to send

`Unity.Notifications.Application` knows how to build, queue, and send an email. It does **not** know that grant applications exist, what an application status is, or what a due date means. That knowledge lives in `Unity.GrantManager.Application`, which publishes `EmailNotificationEvent`s:

| Host component | Trigger |
|---|---|
| `Notifications/EmailAppService` | A user clicks Send or Save Draft in the application email widget |
| `GrantApplications/BulkEmailNotificationAppService` | A user sends a batch of per-application drafts |
| `Events/ScheduledNotificationEventHandler` | An `ApplicationChangedEvent` or `PaymentStatusChangedEvent` matches a configured event-based `ScheduledNotification` |
| `Events/DateBasedScheduledNotificationJob` | A nightly Quartz job finds applications whose configured date field has passed |
| `Logs/ExceptionLogAppService` | A new, not-recently-seen exception is logged (calls `SendEmailNotification` directly, bypassing the queue) |
| `Unity.Payments` `FsbPaymentNotifier` / `FinancialSummaryNotifier` | Payments reach FSB status, or a payment batch fails |

The one inversion of this rule is `EmailNotificationHandler` itself: it lives in `Unity.Notifications.Application/Events/`, but is declared in the `Unity.GrantManager.Events` namespace and injects `IRepository<ScheduledNotification, Guid>` — a host entity. The Notifications project therefore does reference host types, unlike Flex or TenantManagement. See [notifications-roadmap.md](notifications-roadmap.md#the-handler-that-lives-in-the-wrong-module).

## Module layout and dependency direction

```text
Unity.Notifications.Domain.Shared         → enums, feature consts, NotificationsResource localization
        ↑
Unity.Notifications.Domain                → entities, repository interfaces, setting definitions,
                                             NotificationReadStateManager, seed contributors
        ↑
Unity.Notifications.Application.Contracts → DTOs, app service interfaces, NotificationsPermissions
        ↑
Unity.Notifications.Application           → managers/services, CHES client, RabbitMQ producer + consumer,
                                             S3 attachment service, local event handler.
                                             References Unity.GrantManager (ScheduledNotification,
                                             IEndpointManagementAppService, IMarkdownRenderer)
        ↑
Unity.Notifications.EntityFrameworkCore   → ConfigureNotifications() model extension + custom repositories
        ↑
Unity.Notifications.Web                   → Razor Pages, NotificationHub (SignalR), presence tracker,
                                             UnityMessagingController, menus, settings view component
```

The host wires the module in at three points:

- `GrantManagerApplicationModule` → `[DependsOn(typeof(NotificationsApplicationModule))]`
- `GrantManagerEntityFrameworkCoreModule` → `[DependsOn(typeof(NotificationsEntityFrameworkCoreModule))]`, and `GrantTenantDbContext.OnModelCreating` calls `modelBuilder.ConfigureNotifications()`
- `GrantManagerWebModule` → `[DependsOn(typeof(NotificationsWebModule))]`

## Feature and permission gating

Two **tenant features**, both defined in the host's `GrantManagerFeaturesDefinitionProvider` and both defaulting to `false`:

| Feature | Display name | Gates |
|---|---|---|
| `Unity.Notifications` | "Allow Notifications" | Everything email: the handler short-circuits, the main menu item is hidden, the settings tab is hidden, `NotificationListAppService` carries `[RequiresFeature]`, and every scheduled-notification handler checks it first. |
| `Unity.Notifications.DirectMessaging` (`NotificationsFeatureConsts.DirectMessaging`) | "Allow Direct Messaging" | The realtime half only: `NotificationHub.OnConnectedAsync` calls `Context.Abort()` when disabled, the Unity Messaging page and menu item are hidden, and the messaging controller endpoints carry `[RequiresFeature]`. |

Permissions are declared in `NotificationsPermissionDefinitionProvider` under the `Notifications` group, plus one entry grafted onto ABP's `SettingManagement` group. See [notifications-domain-model.md](notifications-domain-model.md#permissions).

Note that the two halves gate differently: email surfaces use `NotificationsPermissions.*`, while every realtime/log surface uses the **role** `IdentityConsts.ITOperationsPermissionName` (`"ITOperations"`, from `modules/Unity.SharedKernel/Permissions/IdentityConsts.cs`) rather than a module-defined permission.

## External dependencies

| System | Used for | Reached through |
|---|---|---|
| **CHES** (Common Hosted Email Service) | Actually sending, status-checking, and cancelling email | `ChesClientService` — base URL from the `DynamicUrl` rows `NOTIFICATION_API_BASE`/`NOTIFICATION_AUTH` via the host's `IEndpointManagementAppService`; client id/secret from `ChesClientOptions`; token cached in `IDistributedCache<TokenValidationResponse, string>` |
| **RabbitMQ** | Decoupling send from request, and retry backoff | `EmailQueueService` (producer) + `EmailConsumer` (`IQueueConsumer<EmailMessages>`), via `Unity.Modules.Shared.MessageBrokers.RabbitMQ` |
| **S3** (object storage) | Email and template attachment bodies | `EmailAttachmentService` with an `IAmazonS3` singleton built from `S3:Endpoint`/`S3:AccessKeyId`/`S3:SecretAccessKey`, path-style addressing |
| **Redis** | SignalR backplane, so realtime messages reach users connected to a different pod | `NotificationsWebModule.ConfigureSignalRRedisBackplane` — standard or Sentinel, channel prefix `Notifications:SignalR:ChannelPrefix` (default `unity:signalr`) |
| **Keycloak/IDIR** | Resolving sender/recipient display names and email addresses | `IExternalUserLookupServiceProvider`, `IIdentityUserRepository`, `IIdentityUserIntegrationService` |

## Core concepts (glossary)

|Term|Meaning|
|---|---|
|**`EmailLog`**|One outbound message. Created before sending, updated after. Holds the rendered body, addresses, CHES response/status/`msgId`, retry count, and links to an application, an applicant, and optionally a `ScheduledNotification`.|
|**`EmailStatus`**|A **string constant class**, not an enum: `Draft`, `Initialized`, `Scheduled`, `Sent`, `Failed`, `Cancelled`. Stored as plain text.|
|**`EmailAction`**|What the publisher wants done — `SendCustom`, `SendEventDriven`, `SendDateDriven`, `SaveDraft`, `SendFailedSummary`, `SendFsbNotification` (plus declared-but-unrouted values). Dispatched in `EmailNotificationHandler.EmailNotificationEventAsync`.|
|**`EmailType`**|How the message was originated — `Manual`, `EventBased`, `DateBased`, `Delayed`. Persisted on the log and rendered as the "Type" column on the Notification List.|
|**`RecipientType`**|`Internal` (staff/email group) or `External` (applicant). Stamped by `EmailNotificationHandler.StampClassificationAsync`.|
|**Email template**|A tenant-scoped `EmailTemplate` row (name, subject, HTML/text body, send-from, recipient category/identifier) that can also own attachments. Tokens in the body are `{{token}}` placeholders resolved against `TemplateVariable` seed rows.|
|**Template variable**|A `TemplateVariable` row mapping a display name and a `{{token}}` to a dotted `MapTo` path on the application (e.g. `applicant_name` → `applicant.applicantName`). Seeded per tenant by `NotificationsDataSeedContributor`.|
|**Email group**|A named, tenant-scoped list of internal users (`EmailGroup` + `EmailGroupUser`) used as the recipient of internal notifications. `FSB-AP` and `Payments` are seeded. A group referenced by an active scheduled notification cannot be deleted.|
|**Email address configuration**|An `EmailAddressConfiguration` row — an allowlisted address with a type (`Sender`, `ReplyTo`, `NoReply`, `Inbound`, `Support`, `Other`). Exactly one per tenant may be `IsDefault`, enforced by a filtered unique index; that one becomes the fallback From address.|
|**`ScheduledNotification`**|A **host** entity (`Unity.GrantManager.Domain/Notifications/`) that configures "when form X's applications reach status Y (or date field Z passes), send template T to recipients R". Not owned by this module. See [notifications-scheduled-notifications.md](notifications-scheduled-notifications.md).|
|**`NotificationLog`**|A durable record of a realtime notification/message delivery, with type, channel, severity, sender, correlation id, and optional exception/environment fields. Written by `NotificationLogsAppService`, read by IT Operations on the Notification Logs page.|
|**`NotificationReadState`**|One row per (tenant, user) holding `LastReadAt` — the watermark the realtime widget uses to compute an unread badge count.|
|**Presence**|In-memory (`ConcurrentDictionary`, singleton `NotificationPresenceTracker`) map of connection → user, keyed `tenantId:userId`. Not backed by Redis, so it is per-pod. See [notifications-realtime.md](notifications-realtime.md#presence-is-per-pod).|

## Read in this order

1. **[notifications-overview.md](notifications-overview.md)** (this file).
2. **[notifications-domain-model.md](notifications-domain-model.md)** — entities, schema, permissions, settings.
3. **[notifications-email-pipeline.md](notifications-email-pipeline.md)** — the send path end to end.
4. **[notifications-attachments.md](notifications-attachments.md)** — S3 attachment lifecycle.
5. **[notifications-scheduled-notifications.md](notifications-scheduled-notifications.md)** — event- and date-driven automation.
6. **[notifications-realtime.md](notifications-realtime.md)** — SignalR, presence, notification logs.
7. **[notifications-web-ui.md](notifications-web-ui.md)** — pages, menus, widgets, gating.
8. **[notifications-roadmap.md](notifications-roadmap.md)** — known rough edges.
