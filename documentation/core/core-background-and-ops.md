# Core — Background Work and Operations

Everything that runs without a user: scheduled workers, health checks, distributed locking, exception logging and alerting, metrics, and the dashboard.

## Background workers

Quartz-based, all deriving from `QuartzBackgroundWorkerBase` and nearly all marked `[DisallowConcurrentExecution]`. Cron expressions come from **settings**, so they are changed through the admin UI rather than a deployment.

### Core workers

| Worker | Setting | Default | Does |
|---|---|---|---|
| `IntakeSyncWorker` | `GrantManager.BackgroundJobs.IntakeResync_Expression` | `0 0 7,19 1/1 * ? *` | Detects CHEFS submissions Unity is missing — see [core-intake.md](core-intake.md#the-nightly-resync) |
| `DataHealthCheckWorker` | `GrantManager.BackgroundJobs.DataHealthCheckMonitor_Expression` | `0 0 * 1/1 * ? *` (hourly) | Data-integrity monitoring across tenants |
| `ApplicantTenantMapReconciliationWorker` | `GrantManager.BackgroundJobs.ApplicantTenantMapReconciliation_Expression` | `0 0 10 1/1 * ? *` | Reconciles the host-side applicant↔tenant map |
| `DateBasedScheduledNotificationJob` | `GrantManager.BackgroundJobs.DateBasedNotificationSchedule_Expression` | | Date-driven emails — see [`notifications/notifications-scheduled-notifications.md`](../notifications/notifications-scheduled-notifications.md) |
| `InboxWorkerBase` / `OutboxWorkerBase` | | | The transactional outbox — see [`transactional-outbox-pattern.md`](../transactional-outbox-pattern.md) |
| `GrantsPortalMessageCleanupWorker` | | | Prunes processed portal messages |

### Module workers

For completeness, the same mechanism is used by modules: `ReconciliationProducer` and `FinancialNotificationSummaryWorker` in Payments ([`payments/payments-cas-integration.md`](../payments/payments-cas-integration.md)), and the AI module's work runs as ABP **background jobs** rather than Quartz workers ([`ai/ai-generation-pipeline.md`](../ai/ai-generation-pipeline.md)).

### The per-tenant sweep pattern

Every worker that touches tenant data follows the same shape, because a Quartz worker starts with no tenant context:

```csharp
var tenants = await _tenantRepository.GetListAsync();
foreach (var tenant in tenants)
{
    using (_currentTenant.Change(tenant.Id, tenant.Name))
    {
        // tenant-scoped work
    }
}
```

Two consequences: the work is **serial across tenants**, so a slow tenant delays the rest; and an unhandled exception inside the loop aborts the remaining tenants unless the worker catches per tenant. Most catch; `IntakeSyncWorker` accumulates its report and continues.

`SettingDefinitions.GetSettingsValue(settingManager, key)` is the shared helper for reading a cron expression, and the convention is to fall back to a hard-coded default if the setting is missing or unreadable rather than failing to construct the worker.

## Distributed locking

`IDistributedLockProvider` (Medallion.Threading) is used wherever concurrent work must be serialised — most visibly by the AI generation queue and the AI rate limiter.

`Application/Locks/` supplies an **in-memory implementation** for environments without a distributed lock backend:

```csharp
public class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    public IDistributedLock CreateLock(string name) => new InMemoryDistributedLock(name, _locks);
}
```

It satisfies the same interface but its guarantees are **per process**. In a multi-pod deployment two pods each hold their own semaphore dictionary, so a lock taken on one does not exclude the other. Anything relying on a lock for correctness across pods needs the real provider configured.

## Health checks

Three, following the Kubernetes convention:

| Check | Probe | Verifies |
|---|---|---|
| `LiveHealthCheck` | liveness | The process is running |
| `ReadyHealthCheck` | readiness | Tenants can be read — i.e. the host database is reachable |
| `StartupHealthCheck` | startup | Startup completed |

`ReadyHealthCheck` is the only one that touches a dependency, and it deliberately touches the cheapest meaningful one.

`DataHealthCheckWorker` is unrelated to these — it is a scheduled data-integrity monitor, not a probe.

## Exception logging

`ExceptionLog` is a host-database entity; `ExceptionLogAppService` (318 lines) writes and reads it.

`CreateAsync` does more than insert. On a new exception it can send an alert email, throttled by a **five-day suppression window per distinct exception**:

```csharp
private const string AlertFromAddress = "NoReply@gov.bc.ca";
private const int AlertEmailSuppressionWindowDays = 5;
```

The email is sent through `IEmailNotificationService` **directly**, bypassing the queued pipeline — one of two paths in the product that do, the other being comment @-mention emails. See [`notifications/notifications-overview.md`](../notifications/notifications-overview.md#the-one-path-that-skips-all-of-it).

`GetListAsync` backs the Exception Logs admin page (`Web/Pages/ExceptionLogs/`).

## Exception notifications

Separate from the log, and more elaborate. Two collectors feed one throttled notifier:

```text
ABP-handled exceptions ──▶ AbpExceptionNotificationSubscriber (IExceptionSubscriber)
                                    │
exceptions bypassing ABP ──▶ ExceptionCounterMiddleware
                                    │
                                    ▼
                        ExceptionNotificationThrottle
                          per-exception-type cooldown
                          global cap: 5 per minute
                                    │
                                    ▼
                        GitHubBlameLookupService ──▶ who last touched the line
                                    │
                                    ▼
                        Teams webhook (DIRECT_MESSAGE_* dynamic URL)
```

Both collectors are needed because ABP handles most exceptions before they reach middleware — the subscriber's own comment says it *"complements `ExceptionCounterMiddleware` which only catches exceptions that bypass ABP"*. The subscriber is registered explicitly in `GrantManagerWebModule.ConfigureServices` rather than by convention.

The throttle is a singleton, documented as *"prevent Teams notification storms during an outage"*, and is thread-safe. The global cap is a hard-coded `GlobalMaxPerMinute = 5`.

`GitHubBlameLookupService` is the unusual part: it queries the GitHub GraphQL API for blame on the failing line so the alert can name a likely owner. Configured via the `GITHUB_REPO` and `GITHUB_GRAPHQL` dynamic URLs.

`ErrorCountingLoggerSink` feeds a Serilog-side error count into the same picture.

## Metrics

Prometheus, via `UseHttpMetrics()` and a `MapMetrics()` endpoint. The endpoint is **authorized**, not public:

```csharp
endpoints.MapMetrics().RequireAuthorization(PolicyRegistrant.MetricsAccessPolicy);
```

`PolicyRegistrant` (`Web/Identity/PolicyRegistrant.cs`) declares that policy — `MetricsAccess` — alongside Unity's other custom authorization policies. It is also what `Web/Controllers/Monitoring/AlertWebhookController.cs` authorizes against.

`Web/Controllers/Monitoring/` holds the remaining monitoring endpoints.

## Dashboard and analytics

`DashboardAppService` (344 lines) aggregates the tenant's application counts, statuses and amounts for the landing page (`Web/Pages/Dashboard/`).

`MatomoUrlProvider` supplies the Matomo tracking URL from the `ANALYTICS_MATOMO_BASE` dynamic URL, returning nothing unless the `Unity.Analytics` tenant feature is enabled. Note there are **two** copies of this type — `Application/Analytics/` and `Web/Analytics/` — and the web one is the one the layout uses.

`Dashboard/Index.cshtml.cs` carries the one expected `CS8604` build warning called out in `CLAUDE.md` — leave it alone unless asked.

## Auditing

ABP's audit log, configured in `GrantManagerWebModule` through `AbpAuditingOptions`, `AbpAspNetCoreAuditingOptions` and `AbpSecurityLogOptions`, with `UseAuditing()` in the pipeline. `EfCoreAuditLogRepository` adds the custom queries the History widget needs.

Entity change tracking is what makes the History widget work, and it is why background consumers that write entities take care to complete their unit of work inside the audit scope — the Payments CAS coordinator documents this explicitly.

## Caching

| Cache | Used for |
|---|---|
| Distributed (Redis) | AI rate-limit cooldowns, payment bulk-action id lists, CHES and CAS tokens, applicant profile cache |
| In-memory | AI operation settings, locality reference data (`RegionalDistrictCache`, `ElectoralDistrictCache`, `EconomicRegionCache`, `CommunitiesCache`) |

Redis also backs data protection and the SignalR backplane when enabled; session middleware is only registered when Redis **and** data protection are both on.
