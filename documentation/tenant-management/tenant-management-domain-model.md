# Tenant Management Domain Model

There is no module-owned domain model here — see [tenant-management-overview.md](tenant-management-overview.md#the-one-big-architectural-fact). This document covers the ABP entities this module extends, how their sensitive data is protected, how the underlying Postgres roles get provisioned, and the permission/policy layer that gates all of it.

## `Tenant` — stock ABP, extended via `ExtraProperties`

`Tenant` is `Volo.Abp.TenantManagement.Tenant` unmodified — no `[ReplaceDbSetType]`, no custom entity, no subclass. `ITenantRepository`/`ITenantManager` are ABP's own. Every domain-specific field this module needs is a flat string in `Tenant.ExtraProperties` (an ABP `ExtraPropertyDictionary`, persisted as a single `text` column, **not** `jsonb`):

|Extra property key|Set by|Meaning|
|---|---|---|
|`DisplayName`|`TenantAppService.CreateAsync`/`UpdateAsync`|Human-readable name shown in the UI (distinct from ABP's own `Tenant.Name`, which is the normalized tenancy identifier).|
|`Division` / `Branch` / `Description`|Same|Free-text ministry org metadata, editable in Create/Edit modals.|
|`CasClientCode`|Same|BC Gov CAS ministry client code — see [tenant-management-application-services.md](tenant-management-application-services.md#cas-client-code-lookup).|
|`LicencePlate`|`TenantAppService.CreateAsync` (from `TenantConnectionStringBuilder.GenerateCredentialsAsync`)|The `T_XXX999` DB/username stem — see below.|
|`PostCreationStepKeys`, `PostCreationStep_{key}_Name/Status/Message/UpdatedAt`|`PostTenantCreationSequenceJob`, seeded by `TenantAppService.CreateAsync`|Per-step post-creation provisioning status — see [tenant-management-post-creation.md](tenant-management-post-creation.md).|

**Why flat strings, not a JSON blob:** every value here is deliberately a scalar. A single JSON-serialized value (e.g. an array) stored in one `ExtraProperties` entry can trip ABP's own Newtonsoft-based change-tracking for that dictionary on the next save (`Unable to cast object of type 'JArray' to type 'JObject'`) — a real, reproducible failure mode. Every property on `Tenant` follows this flat-scalar convention (see [tenant-management-post-creation.md](tenant-management-post-creation.md#why-flat-extraproperties-not-a-json-blob) for the fullest example).

`TenantDto` (`Application.Contracts`) mirrors these as first-class typed properties (`DisplayName`, `Division`, `Branch`, `Description`, `CasClientCode`, `LicencePlate`, `Sections`), mapped explicitly in `UnityTenantManagementMapperlyProfile` (`GetExtraProperty(source, "DisplayName") ?? string.Empty`, etc.) rather than relying on ABP's generic extension-property serialization — a deliberate, explicit mapping choice for every field the UI needs.

## Connection strings — two per tenant, encrypted at rest

Also stock ABP (`TenantConnectionString`, keyed by `Name`), two rows per tenant:

- **`"Tenant"`** (`UnityTenantManagementConsts.TenantConnectionStringName`) — full read-write role.
- **`"Tenant_Readonly"`** (`UnityTenantManagementConsts.TenantReadOnlyConnectionStringName`) — independent read-only role, consumed by the Metabase integration (see [tenant-management-post-creation.md](tenant-management-post-creation.md)).

Both are **AES-256-CBC encrypted at rest** via ABP's built-in `IStringEncryptionService` (PBKDF2/SHA-1, base64-encoded), configured by `StringEncryption:DefaultPassPhrase` in `appsettings.json` (should be an env-var secret in production). Key files:

- Encrypt on write: `TenantAppService.CreateAsync`/`UpdateConnectionStringsAsync`.
- Runtime decrypt: `EncryptedTenantConnectionStringResolver` — replaces ABP's `MultiTenantConnectionStringResolver` via `[Dependency(ReplaceServices = true)]`. Falls back to treating a value as plain text if decryption fails, so pre-encryption rows keep working.
- Migration-time decrypt: `EntityFrameworkCoreGrantManagerDbSchemaMigrator.cs`.
- One-time backfill of pre-existing plain-text rows: `TenantConnectionStringEncryptionMigrator.cs`, run from `GrantManagerDbMigrationService.MigrateAsync()` on startup.
- Admin utility: `scripts/Decrypt-TenantConnectionString.ps1`.

### `TenantConnectionStringBuilder` — how credentials are generated

`Unity.TenantManagement.Application/TenantConnectionStringBuilder.cs` (`ITenantConnectionStringBuilder`, `[RemoteService(false)]`):

- **`GenerateCredentialsAsync()`** — picks a unique `T_XXX999` DB/username (3 random uppercase letters + 3 random digits, `RandomNumberGenerator`/CSPRNG), checked for uniqueness against **currently-existing tenants'** `LicencePlate` extra property only — not against orphaned Postgres roles left behind by a deleted tenant (see [tenant-management-roadmap.md](tenant-management-roadmap.md)). Also generates a fresh 24-character password.
- **`GeneratePassword()`** — 24 characters from `A-Za-z0-9` only. Quote and backslash characters are deliberately excluded **at generation time**, not escaped later, because the password gets interpolated into a single-quoted SQL literal by the migrator — a defense-in-depth choice against SQL injection via a self-generated value, called out explicitly in the source comment.
- **`GenerateReadOnlyCredentials(credentials)`** — same `DbName`, username `+ "_readonly"`, and a **freshly generated, independent** password (not derived from or equal to the read-write password).
- **`Build(tenantName, credentials)`** — string-replaces `Database`/`Username`/`Password` into the `ConnectionStrings:Tenant` config template by key, preserving `Host`/`Port`/`SSL` and original key casing verbatim.

## Postgres role provisioning

`EntityFrameworkCoreGrantManagerDbSchemaMigrator.cs` (host project, `Unity.GrantManager.EntityFrameworkCore`) does the actual Postgres work, invoked **synchronously** from `TenantCreatedEventHandler.HandleEventAsync` — before the deferred post-creation job sequence is even enqueued, so there's no race between provisioning and the Metabase registration step reading the readonly connection string.

- Decrypts the tenant's `"Tenant"` connection string, extracts `Database`/`Username`/`Password` via `NpgsqlConnectionStringBuilder`.
- Validates both identifiers against an allowlist (`EnsureSafeIdentifier`) **before** interpolating them into admin DDL — Postgres identifiers can't be parameterized, so this is the injection guard for a connection string an operator could otherwise have hand-edited via `UpdateConnectionStringsAsync`.
- **`CreateRoleIfNotExistsAsync(adminConnectionString, roleName, password)`**:
  ```sql
  DO $$
  BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{roleName}') THEN
      CREATE ROLE "{roleName}" WITH LOGIN PASSWORD '{password}';
    END IF;
  END
  $$;
  ```
  **Known gap:** the password is only ever set on the `CREATE ROLE` branch. There is no `ALTER ROLE ... PASSWORD` for the "role already exists" path. If a tenant's stored connection-string password is ever changed after the role's first creation (e.g. via `UpdateConnectionStringsAsync`, which only rewrites the encrypted DB row — it never touches Postgres), the actual Postgres role silently keeps its original password. This is a genuine, real desync risk, not a hypothetical — see [tenant-management-roadmap.md](tenant-management-roadmap.md).
- Creates the database if missing; grants full DB/schema/table/sequence privileges to the read-write role and read-only equivalents (`GRANT USAGE`/`SELECT`, `ALTER DEFAULT PRIVILEGES ... GRANT SELECT`) to the readonly role via `GrantReadOnlyPrivilegesAsync`.
- Runs EF migrations as admin, then repeats the role-creation/grant sequence for the readonly connection string if present.

## Permissions and policies

Two distinct **Keycloak roles** sit above the module's own ABP permissions, defined in `modules/Unity.SharedKernel/Permissions/IdentityConsts.cs`:

|Constant|Value|
|---|---|
|`ITAdminPolicyName` / `ITAdminRoleName`|`"ITAdministrator"`|
|`ITOperationsPolicyName` / `ITOperationsRoleName`|`"ITOperations"`|
|`ITAdminOrITOperationsPolicyName`|`"ITAdminOrITOperations"` — role-only OR of the two above|

`TenantManagementPermissions` (`Application.Contracts`) declares the module's own ABP permission names and three composite policy names:

```csharp
public static class Tenants
{
    public const string Create = "UnityTenantManagement.Tenants.Create";
    public const string Update = "UnityTenantManagement.Tenants.Update";
    public const string Delete = "UnityTenantManagement.Tenants.Delete";
    public const string ManageConnectionStrings = "UnityTenantManagement.Tenants.ManageConnectionStrings";
    public const string ManageFeatures = "AbpTenantManagement.Tenants.ManageFeatures";   // note the Abp* prefix
    public const string ManageEndpoints = "AbpTenantManagement.Tenants.ManageEndpoints"; // — a different permission group
}
public static class Policies
{
    public const string TenantsOrITOps = "TenantManagement.TenantsOrITOps";
    public const string TenantsUpdateOrITOps = "TenantManagement.TenantsUpdateOrITOps";
    public const string TenantsCreateOrITOps = "TenantManagement.TenantsCreateOrITOps";
}
```

`ManageFeatures`/`ManageEndpoints` are declared but not observed wired to any explicit `[Authorize]` in this module — see [tenant-management-roadmap.md](tenant-management-roadmap.md).

### `RoleOrPermissionRequirement` — the actual OR logic

None of the composite policies (nor, notably, the raw `Tenants.Create`/`Update` permission names themselves) are gated purely through ABP's normal permission-grant tree. They're registered as custom ASP.NET Core authorization policies in the **host** web project:

`src/Unity.GrantManager.Web/Identity/PolicyRegistrant.cs` (`AddAuthorizationBuilder()`):

```csharp
authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Create,
    policy => policy.AddRequirements(new RoleOrPermissionRequirement(
        ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Create)));
authorizationBuilder.AddPolicy(TenantManagementPermissions.Policies.TenantsCreateOrITOps,
    policy => policy.AddRequirements(new RoleOrPermissionRequirement(
        ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Create)));
```

Both the raw permission name **and** its `...OrITOps` alias are registered with the *identical* requirement, because some Razor Page/toolbar conventions reference the raw name directly while `TenantAppService` uses the composite name — a comment in `PolicyRegistrant.cs` explains this explicitly. The requirement is evaluated by `RoleOrPermissionAuthorizationHandler`:

```csharp
protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleOrPermissionRequirement requirement)
{
    if (requirement.RoleNames.Any(context.User.IsInRole)) { context.Succeed(requirement); return; }
    if (await _permissionChecker.IsGrantedAsync(context.User, requirement.PermissionName)) { context.Succeed(requirement); }
}
```

i.e. succeeds for `ITAdministrator` **or** `ITOperations` role, **or** an individually-granted `Tenants.Create` ABP permission. Practical effect: **IT Operations and IT Administrator can both do almost everything** gated by these composite policies — the one deliberate exception is the "New Tenant" modal/button, tightened to `IdentityConsts.ITAdminPolicyName` alone (a plain role check, no OR), so IT Operations is steered to create tenants only through Onboarding approval instead. See [tenant-management-web-ui.md](tenant-management-web-ui.md) for the full per-page policy table.

`TenantAppService.CreateAsync` also uses `IdentityConsts.ITAdminOrITOperationsPolicyName` directly (not a `TenantManagementPermissions` constant) at the app-service layer to decide whether to strip privileged input fields (`FeatureKeys`, `MetabaseUserEmails`) from a caller who reached the endpoint with only the plain `Tenants.Create` permission but isn't IT Admin/Ops — re-checking the stricter policy server-side so a forged direct API call can't grant arbitrary Metabase access via a caller who only holds the coarse permission.
