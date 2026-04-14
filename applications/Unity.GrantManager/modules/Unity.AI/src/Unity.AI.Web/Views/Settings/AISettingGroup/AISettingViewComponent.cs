using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Settings;

namespace Unity.AI.Web.Views.Settings.AISettingGroup;

[Widget(
    ScriptTypes = [typeof(AISettingScriptBundleContributor)],
    AutoInitialize = true
)]
public class AISettingViewComponent(ISettingProvider settingProvider) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new AISettingViewModel
        {
            AutomaticGenerationEnabled = await settingProvider.GetAsync<bool>(
                AISettings.AutomaticGenerationEnabled, defaultValue: false),
            ManualGenerationEnabled = await settingProvider.GetAsync<bool>(
                AISettings.ManualGenerationEnabled, defaultValue: false)
        };

        return View("~/Views/Settings/AISettingGroup/Default.cshtml", model);
    }

    public class AISettingScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.Add("/Views/Settings/AISettingGroup/Default.js");
        }
    }
}
