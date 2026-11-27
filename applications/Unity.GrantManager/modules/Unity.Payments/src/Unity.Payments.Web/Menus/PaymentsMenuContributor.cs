using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Payments.Web.Menus;

public class PaymentsMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (await featureChecker.IsEnabledAsync("Unity.Payments") && context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var permissionChecker = context.ServiceProvider.GetRequiredService<IPermissionChecker>();
        if (!await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.Default))
        {
            return;
        }

        context.Menu.AddItem(
            new ApplicationMenuItem(
                    PaymentsMenus.Prefix,
                    displayName: "Payments",
                    "~/PaymentRequests"
        ));
    }
}
