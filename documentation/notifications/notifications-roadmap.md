# Known Rough Edges

This module carries more accumulated scaffolding than most — an unfinished subscription model, a Teams integration that was disabled rather than removed, and two halves that were built at different times to different standards. What follows is roughly ordered by real-world risk, not by how much code is involved.

## Retry backoff is an in-process `Task.Delay`

`EmailQueueService.SendToEmailDelayedQueueAsync` implements its backoff as:

```csharp
await Task.Delay(TimeSpan.FromMilliseconds(300000 * (emailNotificationEvent.RetryAttempts + 1)));
await _queueProducer.PublishMessageAsync(message);
```

Five minutes times the attempt number, awaited **inside the consumer's own execution**, before the retry is even published. Three consequences:

1. The consumer thread is parked for the whole delay. `EmailConsumer` calls this from within its unit of work, so a database connection and transaction are held open for up to 55 minutes on the tenth attempt.
2. The delay is process-local. A pod restart during the wait loses the retry entirely — the `EmailLog` stays `Failed` with no queued message to revive it.
3. On later attempts the delay exceeds the message's own `TimeToLive` (20 minutes), so the message may be dead on arrival.

RabbitMQ's delayed-message plugin or a dead-letter-with-TTL pattern would move this into the broker, where it survives restarts and holds nothing open.

## The configured retry limit is not the one that applies

The settings tab exposes **Maximum Email Retry Attempts**, backed by `NotificationsSettings.Mailing.EmailMaxRetryAttempts` (default `3`, `[MaxValue(10)]`). `EmailNotificationService.UpdateSettings` writes it. Nothing reads it.

`EmailConsumer` uses its own `const int maxRetries = 10`. An operator lowering the setting to 1 changes nothing; an email still retries ten times. Either wire the setting into the consumer or remove it from the settings tab — right now it is a control that appears to work and does not.

(Separately, `EmailConsumer.SaveEmailLogWithRetryAsync` has an unrelated `maxRetries = 3` parameter for *concurrency* retries. The two are easy to confuse when reading the file.)

## The hard-coded −7 timezone offset

`EmailNotificationManager.NormalizeToUtc` handles a `DateTimeKind.Unspecified` send time — the kind that comes back from Postgres and from unparsed form input — with:

```csharp
private static readonly TimeSpan BcPermanentDstOffset = TimeSpan.FromHours(-7);
…
_ => new DateTimeOffset(sendOnDateTime, BcPermanentDstOffset).UtcDateTime
```

The name is honest about what it is: British Columbia has legislated permanent daylight time but has not adopted it. Today the province still observes PST (−8) from November to March. A user scheduling an email for 9:00 AM in January gets it delivered at 8:00 AM.

`NotificationListAppService` in the host does this correctly, using `TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")` for its date-range conversion. The two should agree.

## `GetPendingEmailsCountAsync` loads every email log

```csharp
var allEmailLogs = await emailLogsRepository.GetListAsync();
var emailLogs = allEmailLogs.Where(filter.Compile()).ToList();
return emailLogs.Count;
```

The filter is built as an `Expression<Func<EmailLog, bool>>` and then **compiled and applied in memory** rather than passed to the repository. Every `EmailLog` row in the tenant is materialized to produce a single integer. This is called by `DataHealthCheckWorker`, so it runs on a schedule, and its cost grows with the tenant's entire email history. `AsyncExecuter.CountAsync` over the queryable would do it in one round trip.

## Presence is in-process memory behind a Redis backplane

`NotificationPresenceTracker` is a singleton holding `ConcurrentDictionary`s. SignalR messages fan out across pods through the Redis backplane, but presence does not — `GetOnlineUsersAsync` reports only the users connected to the pod that served the request.

In a single-replica deployment this is invisible. With more than one replica, the Unity Messaging online list is a partial view, and the ITOperations user cannot tell which part they are missing. `UnityMessagingController.GetOnlineUsersAsync` partly compensates by listing the full roster and deriving "last activity" from the ABP audit log, but its `IsOnline` flag has the same blind spot.

A Redis-backed presence store (or a periodic presence broadcast that each pod merges) would fix it. Until then, treat the online list as "online on this pod".

## Log types and channels declared for pipelines that don't exist

`NotificationLogType` declares ten values; `NotificationLogChannel` declares four. Only `SignalRDirectMessage` and `SignalR` are ever written. Never written:

- `SignalRSystemNotification`, `SignalRGroupNotification`
- `DbException`, `AbpHandledException`, `MiddlewareUnhandledException`, `UnityException` — with channel `ExceptionPipeline`
- `PrometheusErrorCounterEvent`, `PrometheusExceptionCounterEvent` — with channel `Prometheus`
- `LegacyBridgeEvent`, and the `System` channel

The Notification Logs page offers all of them as filter values, so an operator can filter to `DbException` and always get an empty result. Meanwhile the exception path that *does* exist — `ExceptionLogAppService` — writes to a separate `ExceptionLog` table and sends an alert email, without ever touching `NotificationLog`.

Either route the exception pipeline into `NotificationLogsAppService.CreateAsync` (which is what the schema was clearly shaped for) or trim the enums and the filter dropdowns to what is real.

## The subscription/trigger model is mapped but unused

`Trigger`, `TriggerSubscription`, `Subscriber`, `SubscriptionGroup`, and `SubscriptionGroupSubscription` are fully realised: entities with navigation properties, EF configuration with foreign keys, and tables created by the initial tenant migration. No service, controller, page, or seed contributor references any of them.

They read as a first design for "who gets notified when X happens" that was superseded by `ScheduledNotification` (which lives in the host and uses email groups instead). Five tables and their FKs are being migrated and backed up for nothing. Removing them is a migration, so it needs a deliberate decision rather than a passing cleanup.

## A duplicate `DynamicUrl` stack that nothing registers

`Unity.Notifications` contains its own parallel copy of the endpoint-configuration stack:

- `Unity.Notifications.Domain/Settings/DynamicUrl.cs` (namespace `Unity.GrantManager.Notifications.Settings`)
- `Unity.Notifications.Domain/Settings/IDynamicUrlRepository.cs`
- `Unity.Notifications.EntityFrameworkCore/Repositories/DynamicUrlRepository.cs`
- `Unity.Notifications.EntityFrameworkCore/EntityFrameworkCore/GrantManagerDbContext.cs` — a second `GrantManagerDbContext` with a `DynamicUrls` DbSet, connection string `"Default"`

Nothing outside those four files references that namespace, and the DbContext is never passed to `AddAbpDbContext`. The real `DynamicUrl` is the host's (`Unity.GrantManager.Domain.Shared/Integrations/DynamicUrl.cs`, table `DynamicUrls` in the host `GrantManagerDbContext`), and `ChesClientService` correctly reaches it through the host's `IEndpointManagementAppService`.

Confusingly, the module's shadow `GrantManagerDbContext` shares a class name with the host's real one, so a careless `using` in a future edit could bind to the wrong type.

## The Teams integration is disabled, not removed

`LogNotificationService.PostToNotificationsChannelAsync` has a fully commented-out body and this note:

```
// REWRITE this  - sends to teams channel but we can't anymore
// Would like this to create a push notification to the Unity Notifications service
// which will then send to Teams channel
```

Everything upstream still runs: `MessageCard.GetMessageCard()` reads `wwwroot/teams/MessageCard.json` off disk, `InitializeMessageCard` builds a full Teams MessageCard JSON with facts, and the host's `NotificationsAppService` looks up a Teams channel URL from `DynamicUrl` and logs a warning when none is configured. All of that work is done and then discarded.

Two live callers reach this dead end: `EmailNotificationService.NotifyTeamsChannel` (for CHES errors) and `NotificationsAppService.PostChefsEventToTeamsAsync` (for CHEFS form publish events). Both believe they are alerting someone. Nobody is alerted.

`MessageCard.GetMessageCard` also reads from `Directory.GetCurrentDirectory()` with no existence check, so it throws `FileNotFoundException` if the working directory is not what it assumes — a latent crash in a code path whose output is thrown away.

## Declared-but-unrouted `EmailAction` values

`EmailAction` declares `Retry`, `SendByTemplateId`, `SendApproval`, and `SendDecline`. None appears in `EmailNotificationEventAsync`'s `switch`, and none is published anywhere. Publishing one is a silent no-op: no email, no log row, no warning. If a future caller reaches for `SendApproval` because the name fits, nothing will happen and nothing will say so. A `default:` branch that logs would at least make the failure visible.

`EmailStatus.Created` is likewise declared and never assigned.

## `EmailLog.AssessmentId` and `ApplicantId` are never set

Both are non-nullable `Guid` columns on `EmailLog`. Nothing in the codebase assigns either — every row has `Guid.Empty` in both.

`NotificationListAppService` does read `ApplicantId`:

```csharp
var applicantId = log.ApplicantId != Guid.Empty
    ? log.ApplicantId
    : applicantIdByAppId.GetValueOrDefault(log.ApplicationId);
```

so the fallback branch is the only one ever taken. The code is correct and the list works, but the primary path is dead and the column is noise. `AssessmentId` has no reader at all.

## Email groups are linked to scheduled notifications by name

`ScheduledNotification.RecipientIdentifier` stores email group **names**, and `ScheduledNotificationHelper.GetInternalRecipientEmailAddressesAsync` resolves them with a case-insensitive name match. `EmailGroupsAppService.DeleteAsync` guards deletion of a referenced group, but nothing guards **renaming** one.

Rename `FSB-AP` and every scheduled notification pointing at it silently resolves to zero recipients — the helper logs `"Email group '{GroupName}' not found"` and publishes nothing. No email, no error surfaced to anyone. Storing the group id (as the payment side does) or blocking renames of referenced groups would both close it.

## Setting display names don't resolve

`NotificationsSettingDefinitionProvider` requests `L($"Setting:{settingName}.DisplayName")` where `settingName` is the fully qualified key, e.g. `Setting:GrantManager.Notifications.Mailing.DefaultFromAddress.DisplayName`. The localization file defines `Setting:Notifications.Mailing.DefaultFromAddress.DisplayName` — without the `GrantManager.` segment. `EmailMaxRetryAttempts` and `EnableEmailDelay` have no localization entries at all.

The settings tab renders its own hardcoded `[Display(Name = …)]` labels from `NotificationsSettingViewModel`, so this is invisible there — it only shows up wherever ABP renders the setting definitions themselves.

## The handler that lives in the wrong module

`EmailNotificationHandler.cs` sits in `Unity.Notifications.Application/Events/`, but declares `namespace Unity.GrantManager.Events` and injects `IRepository<ScheduledNotification, Guid>` — a host entity. `EmailGroupsAppService` and `EmailAddressConfigurationsAppService` do the same.

The consequence is architectural rather than functional: `Unity.Notifications.Application` cannot be used without `Unity.GrantManager`, unlike `Unity.Flex` or `Unity.TenantManagement`, both of which invert this (the host implements a contract the module defines). Applying the same inversion here — an `IScheduledNotificationLookup` contract in the module, implemented in the host — would make the module standalone and would put the file where its namespace says it is.

## Miscellaneous smaller items

- **`[AllowAnonymous]` on interface methods.** `IEmailGroupsAppService.GetListAsync` and `IEmailGroupUsersAppService.GetEmailGroupUsersByGroupIdAsync` carry `[AllowAnonymous]` on the **interface** declaration, and `EmailGroupsAppService.GetListAsync` repeats it on the implementation, overriding the class-level `[Authorize]`. Whether anonymous access to the tenant's internal email-group roster is intended is worth confirming.
- **Unenforced permissions.** `Notifications.Email.CancelScheduled` and `Notifications.Email.Schedule` are declared and localized but were not found on any `[Authorize]`; they gate UI visibility only. `Permission:Notifications.Form.Email.Schedule.Delete` is localized with no matching constant.
- **`GetByTenentIdAsync`** — a typo in a public repository method name (`ITemplatesRepository`). Cosmetic, but it propagates to every caller.
- **The `EmailNotificaions` folder** (missing a `t`) in `Unity.Notifications.Application`. The namespace inside is spelled correctly, so only file paths are affected — including the ones cited in these docs.
- **`TemplatesService.cs` declares `TemplateService`**, and `ITemplatesService.cs` declares `ITemplateService`. File names are plural, types are singular.
- **`EmailStatus` is strings, not an enum.** No length constraint, no check constraint, no compile-time exhaustiveness on the `switch`es that read it. Converting it would be a data migration, so it needs planning — but every new status value today is one typo away from a silently unmatched comparison.
- **Test coverage.** `EmailAttachmentServiceTests` is the only test file with tests. The pipeline itself — status transitions, retry classification, CHES response parsing, scheduled-send cancellation — has none, despite being the module's highest-risk logic.
