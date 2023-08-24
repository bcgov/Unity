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
                GrantManagerMenus.Dashboard,
                l["Menu:Dashboard"],
                "~/",
                icon: "fl fl-view-dashboard",
                order: 0
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.GrantPrograms,
                l["Menu:GrantPrograms"],
                url: "/GrantPrograms",
                icon: "fl fl-bank",
                order: 1
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Applications,
                l["Menu:Applications"],
                "~/GrantApplications",
                icon: "fl fl-other-user",
                order: 2
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Scoring,
                l["Menu:Scoring"],
                "~/Payments",
                icon: "fl fl-bullseye",
                order: 3
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Reporting,
                l["Menu:Reporting"],
                "~/",
                icon: "fl fl-lexicon",
                order: 4
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Welcome,
                l["Menu:Welcome"],
                "~/",
                icon: "fl fl-street",
                order: 5
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
