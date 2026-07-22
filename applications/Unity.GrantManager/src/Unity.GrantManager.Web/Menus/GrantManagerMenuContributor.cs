using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.Permissions;
using Unity.Identity.Web.Navigation;
using Unity.Modules.Shared.Navigation;
using Unity.Modules.Shared.Specializations;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement;
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
                icon: "fl fl-other-user",
                order: 1,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            ).OnlyWhenSpecializations(SpecializationConsts.Onboarding)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Applications,
                l["Menu:Applications"],
                "~/GrantApplications",
                icon: "fl fl-other-user",
                order: 1,
                requiredPermissionName: GrantManagerPermissions.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Applicants,
                l["Menu:Applicants"],
                "~/GrantApplicants",
                icon: "fl fl-other-user",
                order: 2,
                requiredPermissionName: GrantApplicationPermissions.Applicants.ViewList
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                UnityIdentityMenuNames.Roles,
                l["Menu:Roles"],
                "~/Identity/Roles",
                icon: "fl fl-settings",
                order: 3,
                requiredPermissionName: IdentityPermissions.Roles.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                UnityIdentityMenuNames.Users,
                l["Menu:Users"],
                "~/Identity/Users",
                icon: "fl fl-other-user",
                order: 4,
                requiredPermissionName: IdentityPermissions.Users.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Intakes,
                l["Menu:Intakes"],
                "~/Intakes",
                icon: "fl fl-settings",
                order: 5,
                requiredPermissionName: GrantManagerPermissions.Intakes.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.ApplicationForms,
                l["Menu:ApplicationForms"],
                "~/ApplicationForms",
                icon: "fl fl-settings",
                order: 6,
                requiredPermissionName: GrantManagerPermissions.ApplicationForms.Default
            )
        );

        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.Dashboard,
                l["Menu:Dashboard"],
                "~/Dashboard",
                icon: "fl fl-view-dashboard",
                order: 7,
                requiredPermissionName: GrantApplicationPermissions.Dashboard.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        // Displayed in the Grant Manager - Used at Tenant Level if the user in the IT Operations role
        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.EndpointManagement,
                displayName: "Endpoints",
                "~/EndpointManagement/Endpoints",
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        // ********************
        // Admin - Tenant Management
        await context.AddItemAsync(
            new ApplicationMenuItem(
                TenantManagementMenuNames.Tenants,
                l["Menu:TenantManagement"],
                "~/TenantManagement/Tenants",
                icon: "fl fl-view-dashboard",
                order: 8,
                requiredPermissionName: TenantManagementPermissions.Tenants.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
        );

        // Tenants list for ITOperations users on the Onboarding tenant
        await context.AddItemAsync(
            new ApplicationMenuItem(
                TenantManagementMenuNames.Tenants,
                l["Menu:TenantManagement"],
                "~/TenantManagement/Tenants",
                icon: "fl fl-view-dashboard",
                order: 8,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            ).OnlyWhenSpecializations(SpecializationConsts.Onboarding)
        );

        // Displayed on the Tenant Management area if the user has the ITAdministrator Role
        await context.AddItemAsync(
            new ApplicationMenuItem(
                GrantManagerMenus.EndpointManagement,
                displayName: "Endpoints",
                "~/EndpointManagement/Endpoints",
                requiredPermissionName: TenantManagementPermissions.Tenants.Default
            ).ExcludeWhenSpecializations(SpecializationConsts.Onboarding)
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
