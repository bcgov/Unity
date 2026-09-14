# Core — Persistence

Unity is multi-tenant with **a separate PostgreSQL database per tenant**, plus one host database. Everything about persistence follows from that.

## Two contexts

| Context | Connection string | Holds |
|---|---|---|
| `GrantManagerDbContext` | `Default` | The host database — tenants, identity, permissions, settings, background jobs, audit logs, plus a handful of Unity host tables |
| `GrantTenantDbContext` | `GrantManagerConsts.DefaultTenantConnectionStringName` | One tenant's data — applications, applicants, forms, assessments, and every module's tenant tables |

Both are registered in `GrantManagerEntityFrameworkCoreModule` with `AddDefaultRepositories(includeAllEntities: true)`, so every entity gets a repository whether or not it is an aggregate root.

### The host context replaces ABP's

```csharp
[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class GrantManagerDbContext : …
```

Identity and tenant management run **through** `GrantManagerDbContext` rather than their own contexts, which is why `Users`, `Roles`, `Tenants` and `TenantConnectionStrings` appear as DbSets on it. `OnModelCreating` then calls the ABP module configurators in sequence — `ConfigurePermissionManagement()`, `ConfigureSettingManagement()`, `ConfigureBackgroundJobs()`, `ConfigureAuditLogging()`, `ConfigureIdentity()`, `ConfigureFeatureManagement()`, `ConfigureTenantManagement()` — plus `ConfigureAI()`.

Unity's own host tables:

| DbSet | Purpose |
|---|---|
| `ApplicantTenantMaps` | Which tenants an applicant exists in — used by the applicant portal |
| `DynamicUrls` | Every external endpoint, resolved at runtime — see [core-integrations.md](core-integrations.md#dynamic-urls) |
| `CasClientCodes` | CAS client configuration per code |
| `TenantTokens` | Per-tenant API tokens |
| `Sectors`, `SubSectors`, `EconomicRegion`, `ElectoralDistricts`, `RegionalDistricts`, `Communities` | Shared locality reference data |
| `InboxMessages`, `OutboxMessages` | The transactional outbox — see [`transactional-outbox-pattern.md`](../transactional-outbox-pattern.md) |
| `AIGenerationRequests`, `AIPrompts`, `AIModels`, `AIOperations` | AI configuration and queue — see [`ai/ai-domain-model.md`](../ai/ai-domain-model.md) |
| `ExceptionLogs` | Application exception log — see [core-background-and-ops.md](core-background-and-ops.md#exception-logging) |

### The tenant context composes the modules

`GrantTenantDbContext.OnModelCreating` is 523 lines: ~30 explicit entity configurations, then the module model extensions:

```csharp
modelBuilder.ConfigureApplicantMerges();   // core, in its own extension file
…
modelBuilder.ConfigurePayments();
modelBuilder.ConfigureFlex();
modelBuilder.ConfigureNotifications();
modelBuilder.ConfigureReporting();
```

That is the whole mechanism by which a module's tables end up in a tenant database. A module that does not appear in this list has no tenant tables — which is why `ConfigureAI()` is called on the *host* context instead.

## Migrations

Two folders, and choosing the wrong one is the most common mistake when adding a table:

```bash
cd applications/Unity.GrantManager/src/Unity.GrantManager.EntityFrameworkCore

dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations
dotnet ef migrations add <Name> --context GrantTenantDbContext  --output-dir Migrations/TenantMigrations
```

| Change | Context |
|---|---|
| Applications, applicants, forms, assessments, attachments, comments, tags | **Tenant** |
| Payments, Flex, Notifications, Reporting tables | **Tenant** |
| `AI.GenerationReviews`, `AI.ApplicationScoresheetAnswers` | **Tenant** |
| Tenants, users, roles, permissions, settings | **Host** |
| Dynamic URLs, CAS client codes, locality reference data | **Host** |
| Inbox/outbox, `AI.AIModels` / `AIOperations` / `AIPrompts` / `AIRequests` | **Host** |

Note the split inside the `AI` schema: it exists in *both* databases holding different tables.

### Flattened migrations

`EntityFrameworkCoreGrantManagerDbSchemaMigrator` carries a mechanism for a squashed migration history, controlled by `Database:FlattenMigrations`:

```csharp
private const string HostInitialMigrationId   = "20260722193713_Initial";
private const string TenantInitialMigrationId = "20260721203242_Initial";
private const string EfCoreProductVersion     = "10.0.3";
```

The problem it solves: a database provisioned before the migrations were flattened has history rows that predate `Initial`, so `Database.MigrateAsync()` would try to re-run `Initial` against a schema that already has those tables. `ReconcileMigrationHistoryAsync` writes the `Initial` row into `__EFMigrationsHistory` first, so EF sees it as already applied and skips it.

It also ensures `__EFMigrationsHistory` exists before migrating, purely so `MigrateAsync` does not log a `CommandError` on a fresh database.

## Provisioning a tenant database

`MigrateAsync(Tenant?)` does considerably more than run migrations. For a tenant it:

1. Reads the tenant's connection string and **decrypts** it via `IStringEncryptionService` — see [`tenant-management/`](../tenant-management/README.md).
2. Validates that Database, Username and Password are all present, throwing `InvalidOperationException` naming the missing one.
3. Builds an **admin** connection string and points a resolved `GrantTenantDbContext` at it with `SetConnectionString`.
4. Grants schema privileges to the tenant role (`GrantSchemaPrivilegesAsync`).
5. Ensures the migration history table exists, reconciles it if flattening is on, and migrates.
6. Grants table privileges (`GrantTablePrivilegesAsync`).
7. Repeats the privilege grants for the **read-only** connection string, if the tenant has one — that is what Reporting and Metabase connect through.

So each tenant database has (at least) two roles: an application role that owns and writes, and a read-only role for reporting.

The migrator resolves both contexts from the service provider deliberately rather than injecting them, so it can retarget the connection string per tenant.

## Multi-tenancy

Almost every core entity implements `IMultiTenant` with a nullable `TenantId`, and ABP's automatic filter does the isolation. **No core query filters on `TenantId` by hand.**

Where the core steps outside it, it does so explicitly with `ICurrentTenant.Change(tenantId)` — almost always a background worker sweeping every tenant:

```csharp
var tenants = await _tenantRepository.GetListAsync();
foreach (var tenant in tenants)
{
    using (_currentTenant.Change(tenant.Id, tenant.Name)) { … }
}
```

`IDataFilter<IMultiTenant>.Disable()` is rarer and appears mainly in the AI module's prompt resolution, where a global fallback row has to be visible from inside a tenant — see [`ai/ai-prompts.md`](../ai/ai-prompts.md#how-a-prompt-is-chosen).

The one entity that deliberately avoids a global filter is `Applicant`, which declares its soft-delete fields by hand rather than implementing `ISoftDelete`. The reason is documented on the entity and repeated in [core-applicants.md](core-applicants.md#why-applicant-is-not-isoftdelete).

## Repositories

Custom repositories live in `Repositories/`; their interfaces sit beside the entities in the Domain project.

| Repository | Lines | Notable for |
|---|---|---|
| `ApplicationRepository` | 677 | The application list query — the widest read path in the product |
| `PermissionRoleMatrixRepository` | 214 | The role/permission matrix behind the permission admin screen |
| `ApplicantRepository` | 205 | Applicant list, lookup and autocomplete — each applying `!a.IsDeleted` by hand |
| `SequenceRepository` | 111 | `GetNextSequenceNumberAsync(prefix)` for Unity application IDs |
| `EfCoreAuditLogRepository` | 66 | Custom audit-log queries for the History widget |
| `ApplicantMergeOperationRepository` | 62 | Merge history and reversibility |
| `TagsRepository`, `AssessmentRepository`, `GenerationReviewRepository`, `InboxMessageRepository`, and per-entity repositories | 40–80 | Narrow query helpers |

`SequenceRepository` is the one to know about: it is why intake wraps application creation in a **transactional** unit of work, since sequence allocation has to be atomic against concurrent submissions.

## Postgres specifics

- **Npgsql legacy timestamps.** `PreConfigureServices` sets `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`, so `timestamp without time zone` is used throughout and `DateTime` values are not implicitly converted to UTC by the driver.
- **`jsonb` columns** carry the semi-structured payloads: `Application.Payload`, `ApplicationFormVersion.FormSchema`, `GenerationReview.ReviewData`, `ApplicationScoresheetAnswers.Answers`, `AIPrompt.MetadataJson`, `AIModel.SettingsJson`, `NotificationLog.PayloadJson`.
- **Entity extensions.** `GrantManagerEfCoreEntityExtensionMappings.Configure()` runs in `PreConfigureServices` and maps ABP's extensible-object properties onto real columns.
- **Error-level SQL logging** — `dbContextConfiguration.DbContextOptions.LogTo(Console.WriteLine, LogLevel.Error)`.

## Startup warm-up

`GrantManagerDbWarmupService` is a background service that pre-warms the EF Core pipeline after startup, and its own doc comment explains the problem precisely: on first use EF compiles the model (30+ entity types), translates the LINQ tree — expensive for the multi-join application list — and establishes the Npgsql pool, together costing **6–8 seconds of cold-start latency** on the first application list request.

It runs in two independent phases:

| Phase | Does | Needs a database? |
|---|---|---|
| 1 — model compilation | Forces `OnModelCreating` and query translation | No; always succeeds |
| 2 — per-tenant round trip | Iterates tenants from the host database and warms Npgsql's pool and Postgres' plan cache for each | Yes |

Phase 2 is configurable through `DbWarmupOptions` (the `DbWarmup` appsettings section), including `IsPhase2Enabled` to skip it entirely.

## Testing

Tests do not need PostgreSQL. Most projects use **SQLite in-memory**; `Unity.GrantManager.Web.Tests` uses `EFCore.InMemory`. That is why repository code sticks to translatable LINQ — a query that only works on Npgsql will fail under SQLite, and several repositories carry suppressions where a Postgres-specific construct was unavoidable.
