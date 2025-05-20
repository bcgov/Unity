namespace Unity.GrantManager.Web.Views.Settings;

using System.Threading.Tasks;
using Unity.GrantManager.Web.Views.Settings.TagManagement;
using Unity.Modules.Shared;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

public class TagManagementPageContributor : SettingPageContributorBase
{
    public TagManagementPageContributor()
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredPermissions(UnitySelector.SettingManagement.Tags.Default);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        context.Groups.Add(
            new SettingPageGroup(
                "GrantManager.TagManagement",
                "Manage Tags",
                typeof(TagManagementViewComponent),
                order: 3
            )
        );

        return Task.CompletedTask;
    }
}
