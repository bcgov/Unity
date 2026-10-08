using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.Permissions;
using Unity.Identity.Web.Navigation;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Navigation;
using Unity.Modules.Shared.Permissions;
using Unity.Modules.Shared.Specializations;
using Unity.TenantManagement.Web.Navigation;
using Volo.Abp.Identity;
using Volo.Abp.UI.Navigation;

namespace Unity.GrantManager.Web.Menus;

public class GrantManagerMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        context.Menu.TryRemoveMenuGroup(UnityIdentityMenuNames.GroupName);
        context.Menu.TryRemoveMenuItem(DefaultMenuNames.Application.Main.Administration);

        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private async static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<GrantManagerResource>();

        await context.AddItemAsync(
            new ApplicationMenuItem(
                TenantManagementMenuNames.Onboarding,
                l["Menu:Onboarding"],
                "~/TenantManagement/Onboarding",
                order: 1
            ).OnlyWhenSpecializations(SpecializationConsts.Onboarding)
            .OnlyWhenInRole(IdentityConsts.ITOperationsRoleName)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Applications,
                l["Menu:Applications"],
                "~/GrantApplications",
                order: 1,
                requiredPermissionName: GrantManagerPermissions.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Applicants,
                l["Menu:Applicants"],
                "~/GrantApplicants",
                order: 2,
                requiredPermissionName: UnitySelector.ApplicantManagement.Applicant.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                UnityIdentityMenuNames.Roles,
                l["Menu:Roles"],
                "~/Identity/Roles",
                order: 3,
                requiredPermissionName: IdentityPermissions.Roles.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                UnityIdentityMenuNames.Users,
                l["Menu:Users"],
                "~/Identity/Users",
                order: 4,
                requiredPermissionName: IdentityPermissions.Users.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Intakes,
                l["Menu:Intakes"],
                "~/Intakes",
                order: 5,
                requiredPermissionName: GrantManagerPermissions.Intakes.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.ApplicationForms,
                l["Menu:ApplicationForms"],
                "~/ApplicationForms",
                order: 6,
                requiredPermissionName: GrantManagerPermissions.ApplicationForms.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Dashboard,
                l["Menu:Dashboard"],
                "~/Dashboard",
                order: 7,
                requiredPermissionName: GrantApplicationPermissions.Dashboard.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        // Displayed in the Grant Manager - Used at Tenant Level for ITAdmin/ITOperations users
        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.EndpointManagement,
                displayName: "Endpoints",
                "~/EndpointManagement/Endpoints"
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
            .OnlyWhenInRole(IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName)
        );

        // ********************
        // Admin - Tenant Management
        await context.AddItemAsync(
            new ApplicationMenuItem(
                TenantManagementMenuNames.Tenants,
                l["Menu:TenantManagement"],
                "~/TenantManagement/Tenants",
                order: 8
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
            .OnlyWhenInRole(IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName)
        );

        // Tenants list for ITOperations users on the Onboarding tenant
        await context.AddItemAsync(
            new ApplicationMenuItem(
                TenantManagementMenuNames.Tenants,
                l["Menu:TenantManagement"],
                "~/TenantManagement/Tenants",
                order: 8
            ).OnlyWhenSpecializations(SpecializationConsts.Onboarding)
            .OnlyWhenInRole(IdentityConsts.ITOperationsRoleName)
        );

        // End Admin ********************
#pragma warning disable S125 // Sections of code should not be commented out
        /* - will complete later after fixing ui sub menu issue */
        //var administration = context.Menu.GetAdministration();

        //if (administration != null)
        //{
        //    if (MultiTenancyConsts.IsEnabled)
        //    {
        //        administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        //    }
        //    else
        //    {
        //        _ = administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        //    }

        //    administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        //    administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);
        //}
        //*/

        //return Task.CompletedTask;
#pragma warning restore S125 // Sections of code should not be commented out
    }
}
