using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.AI.Settings;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Settings;

namespace Unity.AI.Web.Views.Shared.Components.AIConfiguration;

[Widget(
    ScriptTypes = new[] { typeof(AIConfigurationScriptBundleContributor) },
    AutoInitialize = true)]
public class AIConfigurationViewComponent(
    IApplicationFormAppService applicationFormAppService,
    ISettingProvider settingProvider) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid formId)
    {
        var applicationForm = await applicationFormAppService.GetAsync(formId);

        var model = new AIConfigurationViewModel
        {
            ApplicationFormId = formId,
            ShowAutomatic = await settingProvider.GetAsync<bool>(AISettings.AutomaticGenerationEnabled, defaultValue: false),
            ShowManual = await settingProvider.GetAsync<bool>(AISettings.ManualGenerationEnabled, defaultValue: false),
            AutomaticallyGenerateAIAnalysis = applicationForm.AutomaticallyGenerateAIAnalysis,
            ManuallyInitiateAIAnalysis = applicationForm.ManuallyInitiateAIAnalysis
        };

        return View(model);
    }

    public class AIConfigurationScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .Add("/Views/Shared/Scripts/AILegalDisclaimer.js");
            context.Files
                .Add("/Views/Shared/Components/AIConfiguration/Default.js");
        }
    }
}
