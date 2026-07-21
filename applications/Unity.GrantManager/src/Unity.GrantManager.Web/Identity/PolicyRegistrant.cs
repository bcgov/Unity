using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Web.Identity.Authorization;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity.Policy;

internal static class PolicyRegistrant
{
    // IT Administrator is the "host" superuser login (see IdentityProfileLoginAdminHandler) -
    // it must retain at least the host/tenant-admin access ITOperations has, plus a few
    // admin-only permissions (user creation/lookup, tenant delete/connection strings) that
    // used to be granted via a hardcoded claim stamp at login (_adminPermissions, removed
    // when cookie-stamped permission claims were dropped in favour of IPermissionChecker).
    private static readonly string[] ITAdminOrITOperationsRoles =
        [IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName];
    internal const string MetricsAccessPolicy = "MetricsAccess";

    internal static void Register(ServiceConfigurationContext context)
    {
        // All permission-based policies (single permission or otherwise) are resolved
        // dynamically by ABP's AbpAuthorizationPolicyProvider via IPermissionChecker
        // (Redis-cached). Only policies that need to check a Keycloak-issued role claim
        // (IsInRole) need explicit registration here.
        var authorizationBuilder = context.Services.AddAuthorizationBuilder();

        // Metrics endpoint — allow only loopback / RFC-1918 (cluster-internal) callers
        authorizationBuilder.AddPolicy(MetricsAccessPolicy,
            policy => policy.AddRequirements(new InternalNetworkRequirement()));

        // IT Administrator / IT Operations role policies
        authorizationBuilder.AddPolicy(IdentityConsts.ITAdminPolicyName,
            policy => policy.RequireRole(IdentityConsts.ITAdminRoleName));
        authorizationBuilder.AddPolicy(IdentityConsts.ITOperationsPolicyName,
            policy => policy.RequireRole(IdentityConsts.ITOperationsRoleName));
        authorizationBuilder.AddPolicy(IdentityConsts.ITAdminOrITOperationsPolicyName,
            policy => policy.RequireRole(ITAdminOrITOperationsRoles));

        // Tenant management combined: Tenants.<X> OR ITAdmin/ITOperations
        // NOTE: TenantManagementPermissions.Tenants.Default/Create/Update/Delete/ManageConnectionStrings
        // are not real ABP permissions (only ManageFeatures/ManageEndpoints come from the base ABP
        // TenantManagement module - see UnityTenantManagementPermissionDefinitionProvider). They're
        // referenced directly (not via the TenantsXOrITOps composite names) by some Razor Pages/
        // toolbar conventions in UnityTenantManagementWebModule, so both the raw name and its
        // composite-policy equivalent must be registered with the same effective check.
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.ManageFeatures,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.ManageFeatures)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Default,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Default)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Policies.TenantsOrITOps,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Default)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Update,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Update)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Policies.TenantsUpdateOrITOps,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Update)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Create,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Create)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Policies.TenantsCreateOrITOps,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                ITAdminOrITOperationsRoles, TenantManagementPermissions.Tenants.Create)));

        // ITAdmin-only: Tenant delete/connection-string management and Identity user
        // creation/lookup - previously covered by the removed admin claim stamp.
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Delete,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                [IdentityConsts.ITAdminRoleName], TenantManagementPermissions.Tenants.Delete)));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.ManageConnectionStrings,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                [IdentityConsts.ITAdminRoleName], TenantManagementPermissions.Tenants.ManageConnectionStrings)));
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.Create,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                [IdentityConsts.ITAdminRoleName], IdentityPermissions.Users.Create)));
        authorizationBuilder.AddPolicy(IdentityPermissions.UserLookup.Default,
            policy => policy.AddRequirements(new RoleOrPermissionRequirement(
                [IdentityConsts.ITAdminRoleName], IdentityPermissions.UserLookup.Default)));
    }
}

