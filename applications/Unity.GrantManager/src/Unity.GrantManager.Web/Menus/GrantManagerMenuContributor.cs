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
                icon: "ms-Icon--Home",
                order: 0
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.GrantPrograms,
                l["Menu:GrantPrograms"],
                url: "/GrantPrograms",
                icon: "ms-Icon--Bank",
                order: 1
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Applications,
                l["Menu:Applications"],
                "~/GrantApplications",
                icon: "ms-Icon--PageList",
                order: 2
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                GrantManagerMenus.Payments,
                l["Menu:Payments"],
                "~/",
                icon: "ms-Icon--PaymentCard",
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
