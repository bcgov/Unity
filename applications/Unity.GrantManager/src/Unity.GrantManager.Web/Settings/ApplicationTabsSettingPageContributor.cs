using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Settings;
using Unity.GrantManager.Web.Components.ApplicationTabsSettingGroup;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

namespace Unity.GrantManager.Web.Settings;

public class ApplicationTabsSettingPageContributor : SettingPageContributorBase
{
    public ApplicationTabsSettingPageContributor()
    {
        // TODO
        RequiredPermissions(GrantManagerPermissions.ApplicationForms.Default);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        context.Groups.Add(
           new SettingPageGroup(
               SettingsConstants.UI.Tabs.Default,
               "Application Tabs",
               typeof(ApplicationTabsSettingGroupViewComponent),
               order: 1
           )
        );

        return Task.CompletedTask;
    }
}
