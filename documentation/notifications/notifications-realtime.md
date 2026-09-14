# Realtime Messaging and Notification Logs

The second half of the module, sharing nothing with the email pipeline but the schema and the project. It gives IT Operations a live view of who is signed in and a way to push a message into someone's browser immediately, and it keeps a durable log of every delivery.

Gated end to end by the `Unity.Notifications.DirectMessaging` feature (`NotificationsFeatureConsts.DirectMessaging`), and authorized by the **`ITOperations` role** (`IdentityConsts.ITOperationsPermissionName`) rather than by any `NotificationsPermissions` entry.

## `NotificationHub`

`Unity.Notifications.Web/Realtime/NotificationHub.cs` — `[Authorize]`, `[HubRoute("/signalr/notifications")]`.

### Connection lifecycle

`OnConnectedAsync`:

1. **Feature check first.** If `DirectMessaging` is disabled, `Context.Abort()` and return — the connection is refused at the hub, not merely hidden in the UI.
2. Add the connection to `user:{userId}`, and register it with the presence tracker.
3. If a tenant is in context, add it to `tenant:{tenantId}`.
4. If the caller is an ITOperations user, add it to the constant ops group `ops:notification-logs`.
5. Broadcast the updated online list to the ops group and the updated tenant presence list to the tenant group.

`OnDisconnectedAsync` deregisters and re-broadcasts both lists. `HeartbeatAsync` refreshes the connection's `LastActivityUtc` and re-broadcasts tenant presence; the client sends one every 5 minutes and throttles activity-triggered beats to one per minute.

### Group naming

Two static helpers, used everywhere including from outside the hub:

```csharp
NotificationHub.BuildUserGroup(userId)     // "user:{userId}"
NotificationHub.BuildTenantGroup(tenantId) // "tenant:{tenantId}"
```

These strings are also persisted as `NotificationLog.DeliveryTarget`, so a log row records exactly which group the message was pushed to.

### Hub methods

|Method|Authorization|Behaviour|
|---|---|---|
|`GetUnreadMessagesAsync()`|any connected user|Loads up to 50 `SignalRDirectMessage` logs created after the caller's `LastReadAt` that are addressed to them personally **or** are tenant-wide (`UserId == null && TenantId == currentTenant.Id`).|
|`GetConversationHistoryAsync(scope, targetId)`|any connected user|Up to 100 messages. `scope == "tenant"` returns tenant broadcasts; otherwise it returns the two-way thread between the caller and `targetId` (`(UserId == me && Sender == peer) \|\| (UserId == peer && Sender == me)`). Ordered newest-first for the `Take`, then re-sorted oldest-first for display.|
|`MarkMessagesReadAsync()`|any connected user|Advances the caller's `NotificationReadState.LastReadAt`.|
|`GetTenantUsersAsync()`|any connected user|The full tenant roster with online flags.|
|`GetCurrentTenantAsync()`|any connected user|`{ id, name }` or null.|
|`SendPeerMessageAsync(targetUserId, message)`|any connected user|User-to-user within the tenant.|
|`SendTenantMessageAsync(message)`|any connected user|Broadcast to the caller's whole tenant.|
|`GetOnlineUsersAsync()`|**ITOperations only** — throws `HubException` otherwise|Raw presence list across tenants.|
|`SendDirectMessageAsync(targetUserId, message)`|**ITOperations only**|The operator-to-user path.|

All message sends cap at `MaxDirectMessageLength = 4000` characters and reject blank targets or bodies with a `HubException`. `EnableDetailedErrors` is off, so hub exception text does not leak to clients beyond the `HubException` message.

### Cross-tenant safety on peer messages

`SendPeerMessageAsync` looks the target up with `identityUserRepository.FindAsync(targetUserGuid)` and throws if it returns null. The source comment explains why that is sufficient: ABP's automatic multi-tenancy filter applies to the repository query, so a user outside the current tenant comes back null even when the Guid is otherwise valid. There is no manual `TenantId` comparison, and adding one would be redundant.

### Every send is logged

All three send methods push over SignalR **and then** write a `NotificationLog` through `INotificationLogsAppService` with `NotificationType = SignalRDirectMessage`, `Channel = SignalR`, `Severity = Info`, `IsDeliveredRealtime = true`, `CorrelationId = Context.ConnectionId`, `DeliveryTarget` = the group name, and `Environment` from `ASPNETCORE_ENVIRONMENT`. Titles differ per path: `"Direct message sent"`, `"Peer message sent"`, `"Tenant broadcast sent"`.

The push happens before the log write, so a logging failure does not prevent delivery — but it does mean a delivered message can go unrecorded.

### Message type

Tenant broadcasts carry a `messageType` of `"banner"` or `"popup"`, serialized into `NotificationLog.PayloadJson` as `{"messageType":"…"}`. `GetMessageType` reads it back when replaying history, defaulting to `"popup"` on a null, blank, or unparseable payload (`JsonException` is caught and swallowed).

### ITOperations detection

`IsItOperationsUser()` checks four things in order, because the role can arrive in different claim shapes depending on the token:

1. `Context.User.IsInRole(IdentityConsts.ITOperationsRoleName)`
2. Any claim of type `ClaimTypes.Role`, `role`, or `roles` whose value matches (case-insensitive)
3. A Keycloak `realm_access` claim, parsed as JSON, whose `roles` array contains the role
4. Otherwise false — and any parse failure also returns false

## Presence tracking

`NotificationPresenceTracker` (`INotificationPresenceTracker`) is registered as a **singleton** in `NotificationsWebModule`. It holds two `ConcurrentDictionary`s:

- `connectionToUser`: connection id → presence key
- `users`: presence key → `PresenceRecord` (user id, display name, tenant, ITOperations flag, `LastActivityUtc`, and a set of live connection ids)

The presence key is `"{tenantId|host}:{userId}"`, so the same person signed into two tenants counts as two presences. A record is removed only when its last connection drops.

### Presence is per-pod

The `ConcurrentDictionary`s are in process memory. The SignalR **backplane** is Redis-backed, so *messages* reach users on any pod — but *presence* is not, so `GetOnlineUsersAsync` reports only the users connected to the pod serving the request. In a multi-replica deployment the online list is a partial view. See [notifications-roadmap.md](notifications-roadmap.md#presence-is-in-process-memory-behind-a-redis-backplane).

`UnityMessagingController.GetOnlineUsersAsync` compensates in a different way: it takes each tenant's **full user roster** and marks the ones the tracker knows about as online, filling `LastActivityUtc` from the ABP **audit log** (the maximum `ExecutionTime` per user) rather than from presence. Offline users therefore still get a meaningful "last seen".

## Display names

`DisplayNameHelper` resolves a name from three different sources with one rule, so the hub, the controller, and the roster all render the same string:

| Input | Read |
|---|---|
| `ICurrentUser` | `SurName`, `Name`, `UserName` |
| `IdentityUser` | `Surname`, `Name`, `UserName` |
| `ClaimsPrincipal` | `AbpClaimTypes.SurName`, `.Name`, `.UserName` ?? `Identity.Name` |

Combined as `"Surname, Name"` when both exist, otherwise whichever exists, otherwise the username, otherwise the literal `"Unknown User"`.

## The notification log

### Writing — `NotificationLogsAppService`

`[RemoteService(false)]` — in-process only. `CreateAsync` opens its **own transactional unit of work**, inserts the `NotificationLog`, publishes a `NotificationLogCreatedEto` on the local bus, and completes. Returns the new id.

### Fan-out — `NotificationLogCreatedRealtimeHandler`

`ILocalEventHandler<NotificationLogCreatedEto>`, in the Web project, holding an `IHubContext<NotificationHub>`. On every log creation it emits `notificationLogCreated` to three targets:

1. The ops group `ops:notification-logs`
2. The tenant group (`BuildTenantGroup(eventData.TenantId ?? Guid.Empty)` — a host-level log with no tenant lands in the group named `tenant:00000000-…`, which nothing joins)
3. The specific user group, when `UserId` is set

This is what makes the Notification Logs page live: new rows appear without a refresh.

### Reading — `NotificationLogsReadAppService`

`[Authorize(IdentityConsts.ITOperationsPermissionName)]` on the class.

`GetListAsync(GetNotificationLogsInput)` applies:

- `WhereIf(currentTenant.Id.HasValue, x => x.TenantId == currentTenant.Id)` — a tenant-scoped caller sees only their tenant.
- When there is **no** current tenant (host context) and `input.TenantId` is supplied, filter by that instead. This is the deliberate cross-tenant view for host-level operators.
- Optional filters: `UserId`, `NotificationType`, `Severity`, `Channel`, `DateFrom`, `DateTo` (exclusive upper bound via `.Date.AddDays(1)`), and a `SearchText` `Contains` across `Title`, `Message`, `Source`, `CorrelationId`.
- Orders by `CreationTime` descending, applies `SkipCount`/`MaxResultCount`, and projects into `NotificationLogListDto` **in the query** so only the listed columns are fetched.

Display names are then resolved per distinct user id in a loop of `identityUserRepository.FindAsync` calls — one round-trip per user on the page.

`GetAsync(id)` applies the same tenant scoping and throws `EntityNotFoundException` rather than returning null, so a cross-tenant id read is indistinguishable from a nonexistent one.

## HTTP endpoints

### `NotificationRealtimeController` — `api/notifications/realtime`

One endpoint, `[Authorize]`: `GET feature-enabled` returns whether `DirectMessaging` is on. The realtime client JS calls this before attempting a hub connection, so a disabled tenant never opens a socket that would be aborted.

### `UnityMessagingController` — `api/notifications/unity-messaging`

`[Authorize(IdentityConsts.ITOperationsPermissionName)]` at the class level.

|Endpoint|Auth|Notes|
|---|---|---|
|`GET online-users`|ITOperations + `[RequiresFeature]`|Per-tenant roster with online flags and audit-log-derived last activity. In host context it iterates every tenant.|
|`POST message-user`|ITOperations + `[RequiresFeature]`|Operator → one user.|
|`POST message-tenant`|ITOperations + `[RequiresFeature]`|Operator → one tenant.|
|`POST message-tenant-api`|**`[AllowAnonymous]`** + API key|Machine → one tenant.|
|`POST message-all-tenants-api`|**`[AllowAnonymous]`** + API key|Machine → every tenant with the feature enabled.|

### The two API-key endpoints

These exist so an external operations tool can push a maintenance banner without an interactive login. They are `[AllowAnonymous]` at the ASP.NET level and authenticate themselves:

`IsTenantApiKeyValidAsync(tenantId)`:

1. Require an `AuthConstants.ApiKeyHeader` header and a non-empty tenant id.
2. Look up the `TenantToken` row named `TokenConsts.IntakeApiName` for that tenant.
3. Decrypt it with `IStringEncryptionService`.
4. Compare with **`CryptographicOperations.FixedTimeEquals`** on the UTF-8 bytes — constant-time, so the comparison does not leak key material through timing.

`message-tenant-api` additionally re-checks the `DirectMessaging` feature *inside* the target tenant's context before sending, and returns `403` if it is off.

`message-all-tenants-api` authenticates against the **default grant program tenant's** key (`GrantManagerConsts.NormalizedDefaultTenantName`), then iterates every tenant, skipping any where `DirectMessaging` is disabled, and returns the list of tenant ids it actually delivered to.

Both validate that `MessageType` is exactly `banner` or `popup` and enforce the 4000-character cap. `SendTenantMessageAsync` also refuses (`403`) when a tenant-scoped caller targets a different tenant, and returns `404` for an unknown tenant.

## The Redis backplane

`NotificationsWebModule.ConfigureSignalRRedisBackplane` runs only when `Redis:IsEnabled` is true, and supports two shapes:

- **Sentinel** (`Redis:UseSentinel`): builds `ConfigurationOptions` with `ServiceName = Redis:SentinelMasterName`, comma-separated endpoints from `Redis:Configuration`, `AbortOnConnectFail = false`, `AllowAdmin = true`, `DefaultVersion = 7.0.0`, `DefaultDatabase` from `Redis:DatabaseId`.
- **Standard**: `"{Redis:Host}:{Redis:Port}[,password=…],abortConnect=false"`. A missing host or port yields an empty string, and the backplane is silently skipped.

Both set `ChannelPrefix` from `Notifications:SignalR:ChannelPrefix`, defaulting to `unity:signalr`, so multiple environments can share a Redis instance without cross-talk.

## The client

`wwwroot/js/notifications-realtime-client.js` (~1500 lines) builds a floating bubble/panel widget on every page. Notable behaviours:

- **Guarded init** — skips `/account/login`, `/login`, `/splash`, and only initializes once `abp.currentUser.id` exists.
- **Persisted UI state** in `localStorage`: bubble position and hidden flag, panel size and position, and dismissed banners (`unity.notifications.realtime.*` keys).
- **Two modes**, individual and tenant, with per-peer unread counts and a client-side history cache capped at `MAX_HISTORY_ITEMS = 100`.
- **Heartbeats** every 5 minutes, plus activity-triggered beats on `click`/`keydown`/`scroll` throttled to one per minute.
- **Staleness colouring** — green under 10 minutes since last activity, orange under 30, otherwise grey.

Both `signalr.min.js` and `select2.full.js` are added by `NotificationsScriptBundleContributor`.
