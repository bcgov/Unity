using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Web.Identity.Authorization;
using Unity.Modules.Shared;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity.Policy;

// Composite "any of these permissions" policies - no Keycloak role involved.
// TODO: remove once the underlying permissions are consolidated so a single
// real permission can be checked directly instead of an OR across several.
internal static class PermissionOrPolicyRegistrant
{
    internal static void Register(ServiceConfigurationContext context)
    {
        var authorizationBuilder = context.Services.AddAuthorizationBuilder();

        // Applicant Info Logical OR policy
        authorizationBuilder.AddPolicy(UnitySelector.Applicant.UpdatePolicy,
            policy => policy.AddRequirements(new PermissionOrRequirement(
                UnitySelector.Applicant.Summary.Update,
                UnitySelector.Applicant.Contact.Update,
                UnitySelector.Applicant.Authority.Update,
                UnitySelector.Applicant.Location.Update,
                UnitySelector.Applicant.AdditionalContact.Update,

                // NOTE: This will be replaced when Worksheets are normalized with UnitySelector.Applicant.Worksheet.Update
                UnitySelector.Applicant.Default)));

        // Project Info Logical OR policy
        authorizationBuilder.AddPolicy(UnitySelector.Project.UpdatePolicy,
            policy => policy.AddRequirements(new PermissionOrRequirement(
                UnitySelector.Project.Location.Update.Default,
                UnitySelector.Project.Summary.Update.Default,

                // NOTE: This will be replaced when Worksheets are normalized with UnitySelector.Project.Worksheet.Update
                UnitySelector.Project.Default)));
    }
}
