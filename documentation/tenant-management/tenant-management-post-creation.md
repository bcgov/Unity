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
    Task<PostTenantCreationStepCheck> CanExecuteAsync(Guid tenantId) => Task.FromResult(PostTenantCreationStepCheck.Run);
    Task ExecuteAsync(Guid tenantId);
}
```

`PostTenantCreationSequenceJob` (`Unity.GrantManager.Application/Tenants/PostCreation/`) runs the registered steps **one per job execution**, in ascending `Order`, self-re-enqueuing (`IBackgroundJobManager.EnqueueAsync`) for the next `StepIndex` — so the whole sequence is driven entirely by ABP's background job queue, not a long-running loop. `TenantCreatedEventHandler` kicks it off with `StepIndex = 0` right after synchronous provisioning completes.

A step's exception is always caught and logged, never rethrown — ABP's own background-job retry only engages when `ExecuteAsync` throws, and that's deliberately not the recovery mechanism here. `ContinueOnError = true` means a failed step is logged and the sequence moves on to the next step anyway; `false` stops the sequence entirely. Either way, the failed step itself gets no further automatic attempt — recovery is a re-enqueue of a `PostTenantCreationStepArgs` for that step index. For the Metabase step that's the Setup / Resync button on the Configuration modal's Reporting tab (below); there is no generic retry button for a step.

## Status tracking on the tenant

A job whose tenant no longer exists (deleted or purged after the job was queued) logs a warning and returns without running the step, recording a status or enqueuing the next step, so ABP doesn't retry it. Every other outcome is persisted onto the tenant via `TenantPostCreationSectionsExtensions` (`modules/Unity.SharedKernel/PostTenantCreation/`), read back by the Tenants list UI:

- `PostTenantCreationStepStatus` — `Waiting | Success | Failure | Error | Skipped`. `Waiting` is seeded for every step at tenant-creation time (`TenantAppService.CreateAsync` → `SeedPostTenantCreationSections`), so the UI shows something immediately, before the deferred job has even run. `Success`/`Error` are set by `PostTenantCreationSequenceJob` after a step returns/throws. `Skipped` and `Failure` come from `CanExecuteAsync` (next bullet).
- `CanExecuteAsync` returns a `PostTenantCreationStepCheck` (`modules/Unity.SharedKernel/PostTenantCreation/`): `Run`, `Skip(reason)` or `Fail(reason)`. The job records a skip as `Skipped` (the step does not apply) and a fail as `Failure` (the step should apply but cannot run), with the reason as the status message, and does not run the step. A `Failure` stops the sequence when the step's `ContinueOnError` is false, the same as an error. Either can be run again by a manual re-enqueue once the cause is fixed.

### Why flat `ExtraProperties`, not a JSON blob

Status is stored as one flat scalar `ExtraProperties` key per field, not a single JSON-serialized blob: `PostCreationStepKeys` (comma-separated, matching the comma-separated `Metabase.UserEmails` setting elsewhere in this module) plus `PostCreationStep_{key}_Name/Status/Message/UpdatedAt` per step — exactly matching how every other `Tenant.ExtraProperties` entry in this module is a flat string (see [tenant-management-domain-model.md](tenant-management-domain-model.md#tenant--stock-abp-extended-via-extraproperties)). This matters: `Tenant.ExtraProperties` round-trips through EF Core via ABP's own Newtonsoft-based change-tracking for that dictionary, and a string value that itself parses as a JSON array makes the *next* save of that tenant throw `Unable to cast object of type 'JArray' to type 'JObject'` — a real, reproducible failure mode, not a hypothetical one. Keeping every value a plain scalar avoids it entirely. `SetPostTenantCreationStepStatus` also removes a legacy single-key `"Sections"` value if still present on a tenant, as best-effort cleanup for any tenant provisioned before this convention was in place.

The DTO surfaced to the browser (`TenantDto.Sections`) is still a JSON array string — but it's built **fresh at read time** in `UnityTenantManagementMapperlyProfile` from the flat entity fields (`JsonSerializer.Serialize(source.GetPostTenantCreationSections(), ...)`), never written back into `Tenant.ExtraProperties`, so it never re-triggers the bug above.

## The only step today: Metabase registration

`MetabaseTenantRegistrationStep` (`Unity.GrantManager.Application/Tenants/PostCreation/Steps/`, `Order = 1`, `Key = "MetabaseSync"`, `ContinueOnError = true`) holds the checks below; its `ExecuteAsync` calls `MetabaseTenantProvisioner.SetupAsync` (`Unity.GrantManager.Application/Integrations/Metabase/`), which does everything the old manual deployment runbook did by hand:

1. Decrypts the tenant's `Tenant_Readonly` connection string (throws if there is none, so the job records `Error`), saves its username as the tenant's `GrantManager.Reporting.TenantViewRole` setting so generated reporting views are granted to the role Metabase reads as, `FindOrCreateDatabaseAsync`s a Metabase database connection named after the tenant, triggers a schema sync + value rescan. When the connection already exists, `FindOrCreateDatabaseAsync` updates its details with `PUT /api/database/{id}`, so a rerun picks up a changed password or host.
2. `FindOrCreateGroupAsync`s a Metabase permissions group named after the tenant and syncs its members (`SyncMembersAsync`, see [Metabase group members](#metabase-group-members)): adds the program managers and Additional members, and removes off-boarded users. A user not found in Metabase (never logged in via LDAP, no Admin > People entry) is skipped and counted in the log, not a hard failure.
3. Grants that group unrestricted view/query access to the new database connection.
4. `FindOrCreateCollectionAsync`s a Metabase collection for the tenant and grants the group write access.
5. Stores the database, group and collection IDs on the tenant as flat `ExtraProperties` scalars: `MetabaseDatabaseId`, `MetabaseGroupId`, `MetabaseCollectionId`. On a tenant registered before the IDs were stored, the find-or-create calls return the existing resources by name, so running Setup again backfills the IDs.

Once the IDs are stored, a rerun uses them instead of the name lookups: it updates the database by ID (`UpdateDatabaseDetailsAsync`, which also sets the name) and renames the group and collection (`RenameGroupAsync`, `RenameCollectionAsync`) to the tenant's current name. So after a tenant is renamed, Resync renames its Metabase resources rather than creating a second set. Renaming a tenant in the Configuration modal queues this automatically: when the name changed, `OnPostAsync` calls `IMetabaseTenantAppService.ResyncIfSetUpAsync`, which queues Resync only if the tenant has a stored Metabase ID and `CanExecuteAsync` passes (so an environment without Metabase keeps its last result instead of turning it into `Skipped`). Only IT Admin and IT Operations can rename: `TenantAppService.UpdateAsync` refuses a name change from anyone else, so a tenant's Metabase names can't fall behind its own. Only when Metabase returns `404` for a stored ID (deleted by hand) does that resource fall back to find-or-create by name, with a logged warning.

`CanExecuteAsync` checks, in order: `TenantCreation:Steps:Metabase:Enabled` off → `Skipped` (this environment doesn't use Metabase); the tenant lacks the `Unity.Reporting` feature → `Skipped` (it never uses Metabase, so missing configuration doesn't matter to it); no API key, or a missing or blank `METABASE_API_BASE` endpoint → `Failure` with the reason; otherwise the step runs. Every Metabase call is designed to be idempotent (find-or-create by name, membership checks before writing), so a rerun after a partial failure is safe.

### Setup, Resync, Sync schema and Re-scan values from the Reporting tab

`IMetabaseTenantAppService` (`Unity.GrantManager.Application.Contracts/Integrations/Metabase/`, implemented by `MetabaseTenantAppService`, `ITAdminOrITOperationsPolicyName`) backs the Metabase section of the tenant Configuration modal's Reporting tab:

- `GetAsync(tenantId)` returns the step's last status, the stored IDs (shown as links into Metabase), and the result of the same `CanExecuteAsync` checks. When those pass it also calls `GET /api/user/current` with the API key (`IMetabaseApiClient.CheckConnectionAsync`) to confirm Metabase is reachable and the key belongs to an administrator.
- `SetupAsync(tenantId)` marks the step `Waiting` and enqueues `PostTenantCreationStepArgs` at the Metabase step's index, so it runs through `PostTenantCreationSequenceJob` like the original registration. Steps ordered after it would run again too; there are none. The button reads **Setup** when the tenant has no stored IDs and **Resync** when it has; both run the same idempotent Setup.
- `SyncSchemaAsync(tenantId)` and `RescanValuesAsync(tenantId)` call Metabase's `POST /api/database/{id}/sync_schema` and `POST /api/database/{id}/rescan_values` for the tenant's stored `MetabaseDatabaseId`, directly rather than through a background job, because Metabase accepts the request and does the work on its own. Use Sync schema after a reporting view is generated, renamed or deleted, and Re-scan values when filter dropdowns are stale; nothing triggers either automatically after view generation. Their buttons only appear once the tenant has stored IDs, are enabled under the same conditions as Setup, and don't change the step status. With no stored database ID they refuse; if Metabase returns `404` for the ID they say to run Resync.
- `GetMembersAsync`, `SyncMembersAsync`, `AddMembersAsync` and `RemoveMemberAsync` back the members table below the Metabase section; see [Metabase group members](#metabase-group-members). The table loads once the tenant has stored IDs and the connection check passes, and not while Setup is `Waiting`. `RemoveMemberAsync` is a `POST` with the email in the body, so emails stay out of URLs.

### Metabase group members

`MetabaseTenantProvisioner` decides who belongs in the tenant's Metabase group from three sources, matched by email, case-insensitively:

| Source | Shown as | Added by | Removed when |
|---|---|---|---|
| Users with the `program_manager` role | Program manager | Setup, Resync, Sync Members, Add | Off-boarded. Taking the role away from an active user leaves them in the group; the members table then shows them as *Added in Metabase*. |
| The tenant's Additional members list (`MetabaseSettings.UserEmails`, see [tenant-management-application-services.md](tenant-management-application-services.md#metabase-settings)) | Added in Unity | the same | Off-boarded, or **Remove** |
| Anyone else in the group | Added in Metabase | Metabase itself | Off-boarded, or **Remove** |

**Off-boarded** means every user in the tenant with that email is deleted or inactive (`IsActive = false`). An email that also belongs to an active user is not off-boarded, so a user who was deleted and imported again gets access back. Deleted users are read with the `ISoftDelete` filter disabled. An off-boarded email stays on the Additional members list but is skipped, so re-importing or reactivating the user restores access without re-adding them. Metabase administrators see every collection whatever their groups, so off-boarding one is a manual step in Metabase.

`SyncMembersAsync(tenantId, groupId)` reads the group (`GetGroupMembersAsync`, `GET /api/permissions/group/{id}`), removes every member whose email is off-boarded (`RemoveGroupMembershipAsync`, `DELETE /api/permissions/membership/{id}`; a `404` counts as already removed), then adds the program managers and Additional members who are missing and have a Metabase account. It never removes anyone else. Setup and Resync run it, and so does the members table's **Sync Members** button, which runs only this member sync, straight away.

Off-boarding also removes access straight away, without waiting for a sync. `MetabaseMemberOffboardingHandler` listens for a deleted `IdentityUser`, an `IdentityUser` updated while inactive, and ABP's `IdentityUserEmailChangedEto` (Rename can change a user's email; the old email is the one queued). For a tenant with a stored group ID, and only when Metabase is enabled, it queues `MetabaseMemberRemovalJob { TenantId, Email }`. The job removes that email's membership unless the email is still wanted (a program manager or Additional member who is not off-boarded). Metabase errors are rethrown so ABP's background-job retry runs; the next sync catches anything the job missed. Logs record counts and outcomes, never email addresses.

**Remove** on the members table takes the email off the Additional members list and out of the group. It refuses an active program manager, whose access follows their role. **Add** validates the emails, appends them to the Additional members list, then runs the sync.

## `MetabaseApiClient` — handling Metabase's API inconsistencies

`Unity.GrantManager.Application/Integrations/Metabase/MetabaseApiClient.cs` talks to Metabase over plain HTTP via `IResilientHttpRequest` (a Polly-based client that only auto-retries `429`/`500`/`502`/`503`/`504`). Metabase's own API is inconsistent enough that the client has to work around it in three specific ways:

1. **List-endpoint response-shape inconsistency.** Metabase is inconsistent about whether a "list" response is wrapped (`{"data": [...]}`, e.g. `/api/database`) or a raw JSON array (`/api/permissions/group`, `/api/collection`, and the response body of `POST /api/permissions/membership`). `FindIdByNameAsync` reads either shape directly. For call sites that don't need the response body at all (`AddGroupMemberAsync`'s membership POST, `SyncDatabaseSchemaAsync`, `RescanDatabaseValuesAsync`), `PostVoidAsync` validates the HTTP status via `ReadJsonTokenAsync` (which accepts either JSON shape) without ever forcing a `JObject` cast on a body nothing uses.
2. **A racy membership check.** `AddGroupMemberAsync` does check-then-act (`IsGroupMemberAsync` GET, then a POST if not already a member). Under a race — a concurrent/retried registration adding the same membership between the check and the POST — Metabase's own DB rejects the duplicate insert with a raw HTTP `500` (a unique-constraint violation surfacing as an unhandled exception on Metabase's side, not a clean `409`). `AddGroupMemberAsync` catches that POST failure and re-checks membership — if the user is a member now, by whatever path, it's treated as success rather than failing the step.
3. **A transient 422 right after database creation.** `POST /api/database` returns before Metabase has finished asynchronously validating/establishing the connection internally; calling `sync_schema`/`rescan_values` immediately after can transiently 422 (`"Looks like your Password is incorrect"`) even though the connection is fine — and `422` isn't in `IResilientHttpRequest`'s auto-retry list. Both calls retry up to 3 times immediately (no artificial delay, matching the `UpdateGraphWithRetryAsync` retry-on-conflict pattern already in this file for the permissions/collection graph endpoints).

All three behaviors are covered by tests in `test/Unity.GrantManager.Application.Tests/Integrations/Metabase/MetabaseApiClientTests.cs`.

## Status UI on the Tenants list

`modules/Unity.TenantManagement/src/Unity.TenantManagement.Web/Pages/TenantManagement/Tenants/Index.js`:

- A "Setup Status" column renders a single aggregate icon per tenant, rolled up from all tracked steps: green check (every step that applies succeeded), gray minus (every step was skipped), red X (nothing succeeded and at least one failed), amber warning (a mix), gray clock (nothing has finished yet). Skipped steps count as finished, neither succeeded nor failed. A tenant with **no** tracked steps at all (i.e. `PostCreationStepKeys` was never seeded — a legacy tenant created before this feature shipped) renders **no icon**, rather than a permanent "Waiting" clock that would never resolve.
- Clicking the icon opens a SweetAlert2 (`Swal.fire({ html: ... })`) dialog listing every step's name, status, message, and timestamp — pulled from the DataTable's own row data (`_dataTable.row(...).data()`) on click, not embedded as a `data-*` HTML attribute, because the sections JSON contains double quotes that break a naively-escaped double-quoted attribute (`$('<span>').text(json).html()` only escapes for element *content*, not attribute-value context). `abp.message.info` was tried first and rejected — in this app it's just a thin wrapper over the browser's native `alert()`, which can't render markup at all.
- Both the column header and the per-row icon carry a hint tooltip ("click for details") — a native `title` attribute rather than a Bootstrap tooltip, so it works without needing to re-run ABP's tooltip auto-init after every DataTable redraw.
