using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Unity.Reporting.Web.Menus;

public class ReportingMenuContributor : IMenuContributor
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
        //Add main menu items.
        context.Menu.AddItem(new ApplicationMenuItem(ReportingMenus.Prefix, displayName: "Reporting", "~/Reporting", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
