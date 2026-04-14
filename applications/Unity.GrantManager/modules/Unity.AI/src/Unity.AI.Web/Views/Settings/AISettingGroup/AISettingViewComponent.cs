using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.AI.Web.Views.Settings.AISettingGroup;

[Widget(
    ScriptTypes = [typeof(AISettingScriptBundleContributor)],
    AutoInitialize = true
)]
public class AISettingViewComponent : AbpViewComponent
{
    public virtual Task<IViewComponentResult> InvokeAsync()
    {
        return Task.FromResult<IViewComponentResult>(
            View("~/Views/Settings/AISettingGroup/Default.cshtml", new AISettingViewModel()));
    }

    public class AISettingScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.Add("/Views/Settings/AISettingGroup/Default.js");
        }
    }
}
