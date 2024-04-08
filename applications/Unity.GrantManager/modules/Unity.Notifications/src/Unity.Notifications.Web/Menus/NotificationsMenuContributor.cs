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

#pragma warning disable S1172 // Unused method parameters should be removed
#pragma warning disable IDE0060 // Remove unused parameter
    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore S1172 // Unused method parameters should be removed
    {
        // Add main menu items.
        return Task.CompletedTask;
    }
}
