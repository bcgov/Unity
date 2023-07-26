using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.MultiTenancy;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace Unity.GrantManager.Web.Menus;

public class GrantManagerMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<GrantManagerResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                GrantManagerMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.GrantPrograms,
                l["Menu:GrantPrograms"],
                url: "/GrantPrograms",
                icon: "fas fa-award",
                order: 1
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.GrantTracker,
                l["Menu:GrantTracker"],
                icon: "fas fa-file-contract",
                order: 2
            ).AddItem(
                new ApplicationMenuItem(
                    GrantManagerMenus.Applications,
                    l["Menu:Applications"],
                    "~/GrantApplications",
                    order: 0
                )
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Payments,
                l["Menu:Payments"],
                "~/",
                icon: "fas fa-landmark",
                order: 3
            )
        );


        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);

        return Task.CompletedTask;
    }
}
