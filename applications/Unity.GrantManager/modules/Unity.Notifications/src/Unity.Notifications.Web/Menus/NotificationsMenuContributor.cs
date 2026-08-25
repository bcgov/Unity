using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Localization;
using Unity.Notifications.Features;
using Unity.Notifications.Permissions;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Notifications.Web.Menus;

public class NotificationsMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (await featureChecker.IsEnabledAsync("Unity.Notifications")
            && await featureChecker.IsEnabledAsync(NotificationsFeatureConsts.DirectMessaging)
            && context.Menu.Name == StandardMenus.Main)
        {
            ConfigureMainMenu(context);
        }
    }

    private static void ConfigureMainMenu(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<NotificationsResource>();

        context.Menu.AddItem(
            new ApplicationMenuItem(
                NotificationsMenus.NotificationList,
                l["Menu:Notifications"],
                "~/Notifications",
                icon: "fl fl-mail",
                order: 9,
                requiredPermissionName: NotificationsPermissions.NotificationList.View
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                NotificationsMenus.NotificationLogs,
                l["Menu:NotificationLogs"],
                "~/NotificationLogs",
                icon: "fl fl-table",
                order: 10,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            )
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                NotificationsMenus.UnityMessaging,
                l["Menu:RealtimeOps"],
                "~/UnityMessaging",
                icon: "fl fl-users",
                order: 11,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            )
        );
    }
}
