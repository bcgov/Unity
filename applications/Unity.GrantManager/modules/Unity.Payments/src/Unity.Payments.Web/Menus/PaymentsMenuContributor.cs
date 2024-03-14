using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Payments.Web.Menus;

public class PaymentsMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (await featureChecker.IsEnabledAsync("Unity.Payments"))
        {
            if (context.Menu.Name == StandardMenus.Main)
            {
                await ConfigureMainMenuAsync(context);
            }
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, displayName: "Payments", "~/Payments", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
