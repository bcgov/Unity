namespace Unity.Notifications.Web.Views.Settings;

using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Web.Views.Settings.BackgroundJobsSettingGroup;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

public class BackgroundJobsPageContributor : SettingPageContributorBase
{
    public BackgroundJobsPageContributor()
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredPermissions(UnitySettingManagementPermissions.BackgroundJobSettings);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        context.Groups.Add(
            new SettingPageGroup(
                "GrantManager.BackgroundJobs",
                "Background Jobs",
                typeof(BackgroundJobsViewComponent),
                order: 1
            )
        );

        return Task.CompletedTask;
    }
}
