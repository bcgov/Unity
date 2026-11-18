using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Unity.Modules.Shared;
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
        // Existing roles use either the Payments permission or the application payment-list
        // viewing permission. Preserve both ways of granting access to the Payments page.
        if (!await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.Default)
            && !await permissionChecker.IsGrantedAsync(UnitySelector.Payment.PaymentList.Default))
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
