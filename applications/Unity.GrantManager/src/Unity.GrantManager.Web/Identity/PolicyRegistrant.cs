using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Web.Identity.Authorization;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity.Policy;

internal static class PolicyRegistrant
{
    internal static void Register(ServiceConfigurationContext context)
    {
        // Simple permission-based policies (e.g., [Authorize("PermissionName")]) are handled
        // automatically by ABP's AbpAuthorizationPolicyProvider + PermissionRequirementHandler.
        // Only register custom composite policies here.

        var authorizationBuilder = context.Services.AddAuthorizationBuilder();

        // IT Administrator Policy: granted by role OR permission
        authorizationBuilder.AddPolicy(IdentityConsts.ITAdminPolicyName,
            policy => policy.AddRequirements(
                new RoleOrPermissionRequirement(IdentityConsts.ITAdminRoleName, IdentityConsts.ITAdminPermissionName)));

        // IT Operations Policy: granted by role OR permission
        authorizationBuilder.AddPolicy(IdentityConsts.ITOperationsPolicyName,
            policy => policy.AddRequirements(
                new RoleOrPermissionRequirement(IdentityConsts.ITOperationsRoleName, IdentityConsts.ITOperationsPermissionName)));

        // Applicant Info Logical OR policy: any update sub-permission grants access
        authorizationBuilder.AddPolicy(UnitySelector.Applicant.UpdatePolicy,
            policy => policy.AddRequirements(
                new PermissionOrRequirement(
                    UnitySelector.Applicant.Summary.Update,
                    UnitySelector.Applicant.Contact.Update,
                    UnitySelector.Applicant.Authority.Update,
                    UnitySelector.Applicant.Location.Update,
                    UnitySelector.Applicant.AdditionalContact.Update,
                    UnitySelector.Applicant.Default)));

        // Project Info Logical OR policy: any update sub-permission grants access
        authorizationBuilder.AddPolicy(UnitySelector.Project.UpdatePolicy,
            policy => policy.AddRequirements(
                new PermissionOrRequirement(
                    UnitySelector.Project.Location.Update.Default,
                    UnitySelector.Project.Summary.Update.Default,
                    UnitySelector.Project.Default)));
    }
}

