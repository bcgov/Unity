# Known Rough Edges

Unlike Flex's roadmap (one specific, aspirational feature), this module's outstanding issues are a set of independent gaps and inconsistencies in the current implementation. Listed roughly in order of real-world risk.

## Connection-string password desync (compounding, two layers deep)

1. **Postgres role.** `EntityFrameworkCoreGrantManagerDbSchemaMigrator.CreateRoleIfNotExistsAsync` only sets a role's password on the `CREATE ROLE` branch — there is no `ALTER ROLE ... PASSWORD` when the role already exists. See [tenant-management-domain-model.md](tenant-management-domain-model.md#postgres-role-provisioning).
2. **The stored connection string.** `TenantAppService.UpdateConnectionStringsAsync` lets an operator (with `Tenants.ManageConnectionStrings`) edit and re-encrypt a tenant's connection string directly — but it only rewrites the encrypted DB row. It never touches Postgres.

Combined: if a tenant's readonly (or read-write) password is ever changed after the role's first creation — via that operator edit, or via any re-run of tenant-creation logic against a pre-existing role name — the actual Postgres role silently keeps its **original** password while the encrypted connection string (and everything downstream that decrypts and uses it, including the Metabase registration step) reflects the **new** one. The next thing that tries to actually connect fails with an authentication error that gives no hint the mismatch is the cause.

3. **Metabase's own database record compounds this further.** `MetabaseApiClient.FindOrCreateDatabaseAsync` only sends `password` (and the rest of `details`) on the create path — if a Metabase database record for the tenant already exists (the expected, idempotent-by-design case on a re-enqueued/retried registration step), the existing record's stored credentials are never refreshed, even if the tenant's actual readonly password has since changed. There is no `PUT /api/database/{id}` call anywhere in `MetabaseApiClient` to update `details` on an existing record.

None of these three read as bugs in isolation — each is a reasonable "create-only, idempotent-on-existing" implementation. Together, they mean a password change made through the UI silently fails to propagate to either of the two systems that actually need it, with no error at the time of the change — only later, and only if something tries to connect.

## Tenant deletion leaves orphaned resources

`TenantAppService.DeleteAsync` deletes only the ABP `Tenant` row. It does not drop the tenant's Postgres database or roles, and does not deregister anything in Metabase (database connection, permissions group, collection). Two consequences:

- Those resources are simply orphaned — nothing else in the codebase cleans them up.
- `TenantConnectionStringBuilder.GenerateCredentialsAsync`'s uniqueness check for the `T_XXX999` licence-plate stem only checks **currently-existing tenants'** `LicencePlate` extra property — not orphaned Postgres roles left behind by a deleted tenant. A random collision (small but non-zero, given a 3-letter/3-digit space) between a new tenant and a deleted-but-not-cleaned-up one would land `CreateRoleIfNotExistsAsync` on the "role already exists" branch described above, silently keeping the old role's password.

Worth deciding deliberately: either build real cleanup on delete, or make deletion itself a rarer, more guarded operation (e.g. require confirming the Postgres/Metabase side has been handled manually first) — right now it's neither.

## Declared but unenforced permissions

`TenantManagementPermissions.Tenants.ManageFeatures` / `.ManageEndpoints` are declared (note their `AbpTenantManagement.*` prefix, a different permission group from the rest of this module's `UnityTenantManagement.*` permissions) but were not found wired to any explicit `[Authorize]` anywhere in this module. They read as reserved/scaffold constants rather than active gates — worth confirming intent (dead code to remove, or a gap to wire up) before relying on either name meaning anything at runtime.

## Per-tenant endpoint overrides: schema supports it, nothing uses it

`DynamicUrl.TenantId` is nullable, meaning the schema already supports per-tenant API base URL overrides (Metabase, CHEFS, GitHub). Every actual call site found (`MetabaseApiClient.GetBaseUrlAsync`, `GetChefsApiBaseUrlAsync`, `GetGitHubRepoUrlAsync`) passes `tenantSpecific: false`, so in practice these are global-only today. If a future requirement needs a tenant-specific Metabase instance (for example), the data model is ready; the read paths are not.

## Inconsistent authorization on the Reconciliation page

`TenantManagement/Reconciliation/Index.cshtml.cs` uses a plain `[Authorize]` with no specific policy — every other page in this module names an explicit permission or IT role. Not necessarily a security problem (any authenticated user reaching it still needs whatever backs the underlying data), but inconsistent with the rest of the module and worth a deliberate decision either way.

## `PostTenantCreationStepStatus.Failure` is reserved, not reachable

The status enum has four values (`Waiting`, `Success`, `Failure`, `Error`), but only three are currently reachable — the one step that exists today (Metabase registration) either succeeds or throws (`Error`); nothing currently returns a handled, non-exception `Failure`. It's kept in the enum for a future step that needs to report "I ran, and determined this can't succeed" without that being an unhandled exception — not dead code, just unused by the current step count of one.

## No UI for retrying a failed post-creation step

Recovering a failed step (`ContinueOnError = true` logged it and moved on, or `false` stopped the sequence) currently requires an operator to manually re-enqueue a `PostTenantCreationStepArgs` for that tenant/step index directly — there's no button on the Tenants list or the status detail dialog to trigger this. A "retry" action reachable from that dialog would be a natural addition, now that step failures are visible in the UI at all.
