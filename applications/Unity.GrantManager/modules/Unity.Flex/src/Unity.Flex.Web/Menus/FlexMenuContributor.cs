using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Flex.Web.Menus;

public class FlexMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {        
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (await featureChecker.IsEnabledAsync("Unity.Flex") && context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(new ApplicationMenuItem(FlexMenus.Prefix, displayName: "Flex", "~/Flex", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
