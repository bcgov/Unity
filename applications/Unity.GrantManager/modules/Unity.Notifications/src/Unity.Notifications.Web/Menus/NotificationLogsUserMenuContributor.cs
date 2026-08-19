using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Notifications.Web.Menus;

public class NotificationLogsUserMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.User)
        {
            return;
        }

        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
        {
            return;
        }

        var permissionChecker = context.ServiceProvider.GetRequiredService<IPermissionChecker>();

        if (!await permissionChecker.IsGrantedAsync(IdentityConsts.ITOperationsPermissionName))
        {
            return;
        }

        var l = context.GetLocalizer<NotificationsResource>();

        context.Menu.AddItem(
            new ApplicationMenuItem(
                NotificationsMenus.NotificationLogs,
                l["Menu:NotificationLogs"],
                "~/NotificationLogs",
                icon: "fl fl-table",
                order: 90,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            )
        );
    }
}
