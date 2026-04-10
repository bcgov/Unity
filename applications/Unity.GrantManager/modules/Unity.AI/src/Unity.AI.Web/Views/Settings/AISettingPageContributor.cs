using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.AI.Web.Views.Settings.AISettingGroup;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

namespace Unity.AI.Web.Views.Settings;

public class AISettingPageContributor : SettingPageContributorBase
{
    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredPermissions(AIPermissions.Configuration.ConfigureAI);

        context.Groups.Add(
            new SettingPageGroup(
                "AI.Configuration",
                "AI Configuration",
                typeof(AISettingViewComponent),
                order: 5
            )
        );

        return Task.CompletedTask;
    }
}
