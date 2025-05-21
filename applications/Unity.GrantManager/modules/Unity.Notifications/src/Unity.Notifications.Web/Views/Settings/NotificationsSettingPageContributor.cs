namespace Unity.Notifications.Web.Views.Settings;

using System.Threading.Tasks;
using Unity.Notifications.Permissions;
using Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

public class NotificationsSettingPageContributor : SettingPageContributorBase
{
    public NotificationsSettingPageContributor()
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredTenantSideFeatures("Unity.Notifications");
        RequiredPermissions(NotificationsPermissions.Settings);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        context.Groups.Add(
            new SettingPageGroup(
                "GrantManager.Notifications",
                "Notifications",
                typeof(NotificationsSettingViewComponent),
                order: 2
            )
        );

        return Task.CompletedTask;
    }
}
