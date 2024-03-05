using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Unity.Payments.Web.Menus;

public class PaymentsMenuContributor : IMenuContributor
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
        context.Menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, displayName: "Payments", "~/Payments", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
