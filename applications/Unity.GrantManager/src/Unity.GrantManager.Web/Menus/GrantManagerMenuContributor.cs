using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.Permissions;
using Unity.Identity.Web.Navigation;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement;
using Unity.TenantManagement.Web.Navigation;
using Volo.Abp.Identity;
using Volo.Abp.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp.Features;

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
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Applications,
                l["Menu:Applications"],
                "~/GrantApplications",
                icon: "fl fl-other-user",
                order: 1,
                requiredPermissionName: GrantManagerPermissions.Default
            )
        );

        context.Menu.AddItem(
               new ApplicationMenuItem(
                   UnityIdentityMenuNames.Roles,
                   l["Menu:Roles"],
                   "~/Identity/Roles",
                   icon: "fl fl-settings",
                   order: 2,
                   requiredPermissionName: IdentityPermissions.Roles.Default
               )
           );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                UnityIdentityMenuNames.Users,
                l["Menu:Users"],
                "~/Identity/Users",
                icon: "fl fl-other-user",
                order: 3,
                requiredPermissionName: IdentityPermissions.Users.Default
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Intakes,
                l["Menu:Intakes"],
                "~/Intakes",
                icon: "fl fl-settings",
                order: 4,
                requiredPermissionName: GrantManagerPermissions.Intakes.Default
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.ApplicationForms,
                l["Menu:ApplicationForms"],
                "~/ApplicationForms",
                icon: "fl fl-settings",
                order: 5,
                requiredPermissionName: GrantManagerPermissions.ApplicationForms.Default
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Dashboard,
                l["Menu:Dashboard"],
                "~/Dashboard",
                icon: "fl fl-view-dashboard",
                order: 7,
                requiredPermissionName: GrantApplicationPermissions.Dashboard.Default
            )
        );
        // Displayed in the Grant Manager - Used at Tenant Level if the user in the IT Operations role
        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.EndpointManagement,
                displayName: "Endpoints",
                "~/EndpointManagement/Endpoints",
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            )
        );

        if (await featureChecker.IsEnabledAsync("Unity.AIReporting"))
        {
            context.Menu.AddItem(
                new ApplicationMenuItem(
                    GrantManagerMenus.AIReporting,
                    l["Menu:AIReporting"],
                    "~/AIReporting",
                    icon: "fl fl-view-dashboard",
                    order: 9
                )
            );
        }

        // ********************
        // Admin - Tenant Management 
        context.Menu.AddItem(
          new ApplicationMenuItem(
              TenantManagementMenuNames.Tenants,
              l["Menu:TenantManagement"],
              "~/TenantManagement/Tenants",
              icon: "fl fl-view-dashboard",
              order: 8,
              requiredPermissionName: TenantManagementPermissions.Tenants.Default
          )
        );

        // Displayed on the Tenant Managment area if the user has the ITAdministrator Role
        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.EndpointManagement,
                displayName: "Endpoints",
                "~/EndpointManagement/Endpoints",
                requiredPermissionName: TenantManagementPermissions.Tenants.Default
            )
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
