# Post-Tenant-Creation Steps and Status Tracking

Creating the ABP `Tenant` row, its Postgres database, and its roles happens synchronously during `TenantAppService.CreateAsync` and the `TenantCreatedEventHandler` it triggers. Everything else that needs to happen for a *fully usable* tenant — currently, registering it with Metabase for reporting access — happens **asynchronously**, as a deferred background job sequence, because it depends on external systems (Metabase's API) that shouldn't block the tenant-creation request or retry indefinitely inline.

## The step sequence

`IPostTenantCreationStep` (`modules/Unity.SharedKernel/PostTenantCreation/IPostTenantCreationStep.cs`) — implement this in any module and register as `ITransientDependency` to be picked up automatically, no changes needed to the job that runs them:

```csharp
public interface IPostTenantCreationStep
{
    int Order { get; }
    string Key { get; }        // stable, machine-readable — must not change once shipped
    string StepName { get; }   // human-readable, used in logs and the status UI
    bool ContinueOnError { get; }
    Task<bool> CanExecuteAsync(Guid tenantId) => Task.FromResult(true);
    Task ExecuteAsync(Guid tenantId);
}
```

`PostTenantCreationSequenceJob` (`Unity.GrantManager.Application/Tenants/PostCreation/`) runs the registered steps **one per job execution**, in ascending `Order`, self-re-enqueuing (`IBackgroundJobManager.EnqueueAsync`) for the next `StepIndex` — so the whole sequence is driven entirely by ABP's background job queue, not a long-running loop. `TenantCreatedEventHandler` kicks it off with `StepIndex = 0` right after synchronous provisioning completes.

A step's exception is always caught and logged, never rethrown — ABP's own background-job retry only engages when `ExecuteAsync` throws, and that's deliberately not the recovery mechanism here. `ContinueOnError = true` means a failed step is logged and the sequence moves on to the next step anyway; `false` stops the sequence entirely. Either way, the failed step itself gets no further automatic attempt — recovery is a manual re-enqueue of a `PostTenantCreationStepArgs` for that step index (there's currently no UI button for this; an operator does it directly).

## Status tracking on the tenant

Every outcome (aside from a `CanExecuteAsync`-driven skip — see below) is persisted onto the tenant via `TenantPostCreationSectionsExtensions` (`modules/Unity.SharedKernel/PostTenantCreation/`), read back by the Tenants list UI:

- `PostTenantCreationStepStatus` — `Waiting | Success | Failure | Error`. `Waiting` is seeded for every step at tenant-creation time (`TenantAppService.CreateAsync` → `SeedPostTenantCreationSections`), so the UI shows something immediately, before the deferred job has even run. `Success`/`Error` are set by `PostTenantCreationSequenceJob` after a step returns/throws. `Failure` is reserved for a future step that reports a handled, non-exception failure — no current step produces it, since the only step today (Metabase registration) either succeeds or throws.
- A step skipped via `CanExecuteAsync` returning `false` (e.g. no Metabase API key configured) is left as `Waiting`, not marked as any terminal result — it may still run on a later manual re-enqueue once the missing precondition is fixed.

### Why flat `ExtraProperties`, not a JSON blob

Status is stored as one flat scalar `ExtraProperties` key per field, not a single JSON-serialized blob: `PostCreationStepKeys` (comma-separated, matching the existing `MetabaseUserEmails` convention elsewhere in this module) plus `PostCreationStep_{key}_Name/Status/Message/UpdatedAt` per step — exactly matching how every other `Tenant.ExtraProperties` entry in this module is a flat string (see [tenant-management-domain-model.md](tenant-management-domain-model.md#tenant--stock-abp-extended-via-extraproperties)). This matters: `Tenant.ExtraProperties` round-trips through EF Core via ABP's own Newtonsoft-based change-tracking for that dictionary, and a string value that itself parses as a JSON array makes the *next* save of that tenant throw `Unable to cast object of type 'JArray' to type 'JObject'` — a real, reproducible failure mode, not a hypothetical one. Keeping every value a plain scalar avoids it entirely. `SetPostTenantCreationStepStatus` also removes a legacy single-key `"Sections"` value if still present on a tenant, as best-effort cleanup for any tenant provisioned before this convention was in place.

The DTO surfaced to the browser (`TenantDto.Sections`) is still a JSON array string — but it's built **fresh at read time** in `UnityTenantManagementMapperlyProfile` from the flat entity fields (`JsonSerializer.Serialize(source.GetPostTenantCreationSections(), ...)`), never written back into `Tenant.ExtraProperties`, so it never re-triggers the bug above.

## The only step today: Metabase registration

`MetabaseTenantRegistrationStep` (`Unity.GrantManager.Application/Tenants/PostCreation/Steps/`, `Order = 1`, `Key = "MetabaseSync"`, `ContinueOnError = true`) does everything the old manual deployment runbook did by hand:

1. Decrypts the tenant's `Tenant_Readonly` connection string, `FindOrCreateDatabaseAsync`s a Metabase database connection named after the tenant, triggers a schema sync + value rescan.
2. `FindOrCreateGroupAsync`s a Metabase permissions group named after the tenant, adds the configured member emails (from the per-tenant `MetabaseSettings.UserEmails` snapshot — see [tenant-management-application-services.md](tenant-management-application-services.md#metabase-settings)) via `AddGroupMemberAsync`. A user not found in Metabase (never logged in via LDAP, no Admin > People entry) is skipped with a logged warning, not a hard failure.
3. Grants that group unrestricted view/query access to the new database connection.
4. `FindOrCreateCollectionAsync`s a Metabase collection for the tenant and grants the group write access.

`CanExecuteAsync` returns `false` (skip, not failure) when no Metabase API key is configured — lets the whole feature be a no-op in environments without Metabase wired up. Every Metabase call is designed to be idempotent (find-or-create by name, membership checks before writing), so a manual re-enqueue after a partial failure is safe.

## `MetabaseApiClient` — handling Metabase's API inconsistencies

`Unity.GrantManager.Application/Integrations/Metabase/MetabaseApiClient.cs` talks to Metabase over plain HTTP via `IResilientHttpRequest` (a Polly-based client that only auto-retries `429`/`500`/`502`/`503`/`504`). Metabase's own API is inconsistent enough that the client has to work around it in three specific ways:

1. **List-endpoint response-shape inconsistency.** Metabase is inconsistent about whether a "list" response is wrapped (`{"data": [...]}`, e.g. `/api/database`) or a raw JSON array (`/api/permissions/group`, `/api/collection`, and the response body of `POST /api/permissions/membership`). `FindIdByNameAsync` reads either shape directly. For call sites that don't need the response body at all (`AddGroupMemberAsync`'s membership POST, `SyncDatabaseSchemaAsync`, `RescanDatabaseValuesAsync`), `PostVoidAsync` validates the HTTP status via `ReadJsonTokenAsync` (which accepts either JSON shape) without ever forcing a `JObject` cast on a body nothing uses.
2. **A racy membership check.** `AddGroupMemberAsync` does check-then-act (`IsGroupMemberAsync` GET, then a POST if not already a member). Under a race — a concurrent/retried registration adding the same membership between the check and the POST — Metabase's own DB rejects the duplicate insert with a raw HTTP `500` (a unique-constraint violation surfacing as an unhandled exception on Metabase's side, not a clean `409`). `AddGroupMemberAsync` catches that POST failure and re-checks membership — if the user is a member now, by whatever path, it's treated as success rather than failing the step.
3. **A transient 422 right after database creation.** `POST /api/database` returns before Metabase has finished asynchronously validating/establishing the connection internally; calling `sync_schema`/`rescan_values` immediately after can transiently 422 (`"Looks like your Password is incorrect"`) even though the connection is fine — and `422` isn't in `IResilientHttpRequest`'s auto-retry list. Both calls retry up to 3 times immediately (no artificial delay, matching the `UpdateGraphWithRetryAsync` retry-on-conflict pattern already in this file for the permissions/collection graph endpoints).

All three behaviors are covered by tests in `test/Unity.GrantManager.Application.Tests/Integrations/Metabase/MetabaseApiClientTests.cs`.

## Status UI on the Tenants list

`modules/Unity.TenantManagement/src/Unity.TenantManagement.Web/Pages/TenantManagement/Tenants/Index.js`:

- A "Setup Status" column renders a single aggregate icon per tenant, rolled up from all tracked steps: green check (all succeeded), red X (nothing succeeded and at least one failed), amber warning (a mix), gray clock (nothing has finished yet). A tenant with **no** tracked steps at all (i.e. `PostCreationStepKeys` was never seeded — a legacy tenant created before this feature shipped) renders **no icon**, rather than a permanent "Waiting" clock that would never resolve.
- Clicking the icon opens a SweetAlert2 (`Swal.fire({ html: ... })`) dialog listing every step's name, status, message, and timestamp — pulled from the DataTable's own row data (`_dataTable.row(...).data()`) on click, not embedded as a `data-*` HTML attribute, because the sections JSON contains double quotes that break a naively-escaped double-quoted attribute (`$('<span>').text(json).html()` only escapes for element *content*, not attribute-value context). `abp.message.info` was tried first and rejected — in this app it's just a thin wrapper over the browser's native `alert()`, which can't render markup at all.
- Both the column header and the per-row icon carry a hint tooltip ("click for details") — a native `title` attribute rather than a Bootstrap tooltip, so it works without needing to re-run ABP's tooltip auto-init after every DataTable redraw.
