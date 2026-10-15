# Known Rough Edges

Unlike Flex's roadmap (one specific, aspirational feature), this module's outstanding issues are a set of independent gaps and inconsistencies in the current implementation. Listed roughly in order of real-world risk.

## Connection-string password desync (compounding, two layers deep)

1. **Postgres role.** `EntityFrameworkCoreGrantManagerDbSchemaMigrator.CreateRoleIfNotExistsAsync` only sets a role's password on the `CREATE ROLE` branch — there is no `ALTER ROLE ... PASSWORD` when the role already exists. See [tenant-management-domain-model.md](tenant-management-domain-model.md#postgres-role-provisioning).
2. **The stored connection string.** `TenantAppService.UpdateConnectionStringsAsync` lets an operator (with `Tenants.ManageConnectionStrings`) edit and re-encrypt a tenant's connection string directly — but it only rewrites the encrypted DB row. It never touches Postgres.

Combined: if a tenant's readonly (or read-write) password is ever changed after the role's first creation — via that operator edit, or via any re-run of tenant-creation logic against a pre-existing role name — the actual Postgres role silently keeps its **original** password while the encrypted connection string (and everything downstream that decrypts and uses it, including the Metabase registration step) reflects the **new** one. The next thing that tries to actually connect fails with an authentication error that gives no hint the mismatch is the cause.

Neither reads as a bug in isolation — each is a reasonable "create-only, idempotent-on-existing" implementation. Together, they mean a password change made through the UI silently fails to reach the Postgres role, with no error at the time of the change — only later, and only if something tries to connect. Metabase Resync (Reporting tab) copies the stored connection string's details onto Metabase's database record, which doesn't help while the role still has the old password.

## Tenant deletion: what purge does not clean up

Deleting a tenant is two steps: `DeleteAsync` soft-deletes it, and `TenantAppService.PurgeAsync` drops its database, roles and host records (see [tenant-management-application-services.md](tenant-management-application-services.md)). Gaps that remain:

- **Metabase is never deregistered.** The database connection, permissions group and collection created by the post-creation step stay behind. `MetabaseApiClient` has no delete calls, and the IDs it creates are not stored, so a safe delete would first need those IDs persisted — a name-based delete could hit another environment's resources where environments share one Metabase. Purge logs a warning when a post-creation step had succeeded.
- **Seeded tenants can't be purged.** They have no `LicencePlate` and point at configured databases, so `PurgeAsync` refuses them; soft-deleting one also lets the DbMigrator seeder create a new copy beside it (`FindByNameAsync` ignores soft-deleted rows).

## Declared but unenforced permissions

`TenantManagementPermissions.Tenants.ManageFeatures` / `.ManageEndpoints` are declared (note their `AbpTenantManagement.*` prefix, a different permission group from the rest of this module's `UnityTenantManagement.*` permissions) but were not found wired to any explicit `[Authorize]` anywhere in this module. They read as reserved/scaffold constants rather than active gates — worth confirming intent (dead code to remove, or a gap to wire up) before relying on either name meaning anything at runtime.

## Per-tenant endpoint overrides: schema supports it, nothing uses it

`DynamicUrl.TenantId` is nullable, meaning the schema already supports per-tenant API base URL overrides (Metabase, CHEFS, GitHub). Every actual call site found (`MetabaseApiClient.GetBaseUrlAsync`, `GetChefsApiBaseUrlAsync`, `GetGitHubRepoUrlAsync`) passes `tenantSpecific: false`, so in practice these are global-only today. If a future requirement needs a tenant-specific Metabase instance (for example), the data model is ready; the read paths are not.

## Inconsistent authorization on the Reconciliation page

`TenantManagement/Reconciliation/Index.cshtml.cs` uses a plain `[Authorize]` with no specific policy — every other page in this module names an explicit permission or IT role. Not necessarily a security problem (any authenticated user reaching it still needs whatever backs the underlying data), but inconsistent with the rest of the module and worth a deliberate decision either way.

## No UI for retrying a failed post-creation step

The Metabase step can be re-run from the Configuration modal's Reporting tab (Set up / Resync), but there's no retry action on the Tenants list or in the Setup Status dialog, and none for a step in general. Any future step would need its own way to be re-run, or a generic "retry" action reachable from that dialog.
