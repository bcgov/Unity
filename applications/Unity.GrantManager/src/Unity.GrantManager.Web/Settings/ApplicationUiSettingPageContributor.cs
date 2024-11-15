using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Settings;
using Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

namespace Unity.GrantManager.Web.Settings;

public class ApplicationUiSettingPageContributor : SettingPageContributorBase
{
    public ApplicationUiSettingPageContributor()
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredPermissions(UnitySettingManagementPermissions.UserInterface);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        context.Groups.Add(
           new SettingPageGroup(
               SettingsConstants.UI.Tabs.Default,
               "Application Tabs",
               typeof(ApplicationUiSettingGroupViewComponent),
               order: 1
           )
        );
        return Task.CompletedTask;
    }
}
