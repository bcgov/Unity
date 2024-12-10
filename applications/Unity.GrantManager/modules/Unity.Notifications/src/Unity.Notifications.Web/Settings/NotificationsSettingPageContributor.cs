namespace Unity.Notifications.Web.Settings;

using System.Threading.Tasks;
using Unity.Notifications.Permissions;
using Unity.Notifications.Web.Settings.NotificationsSettingGroup;
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
                typeof(NotificationsSettingViewComponent)
            )
        );

        return Task.CompletedTask;
    }
}
