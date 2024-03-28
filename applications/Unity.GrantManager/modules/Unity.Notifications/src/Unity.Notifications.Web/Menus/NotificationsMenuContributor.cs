using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Unity.Notifications.Web.Menus;

public class NotificationsMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(new ApplicationMenuItem(NotificationsMenus.Prefix, displayName: "Notifications", "~/Notifications", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
