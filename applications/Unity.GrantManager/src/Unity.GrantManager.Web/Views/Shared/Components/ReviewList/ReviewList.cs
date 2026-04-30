using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Views.Shared.Components.ReviewList
{
    [Widget(
        ScriptFiles = new[]
        {
            "/Pages/GrantApplications/ai-generation-button-state.js",
            "/Views/Shared/Components/ReviewList/ReviewList.js"
        },
        StyleFiles = new[]
        {
            "/Views/Shared/Components/ReviewList/ReviewList.css"
        })]
    public class ReviewList(
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker,
        IApplicationFormRepository applicationFormRepository) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationFormId)
        {
            var scoringFeatureEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

            var settingProvider = LazyServiceProvider.LazyGetRequiredService<ISettingProvider>();
            var tenantManualEnabled = await settingProvider.GetAsync<bool>(AISettings.ManualGenerationEnabled, defaultValue: false);

            var applicationForm = await applicationFormRepository.GetAsync(applicationFormId);

            ViewBag.IsAIScoringEnabled =
                scoringFeatureEnabled &&
                tenantManualEnabled &&
                applicationForm.ManuallyInitiateAIAnalysis &&
                await permissionChecker.IsGrantedAsync(AIPermissions.Analysis.GenerateScoring);

            return View();
        }
    }
}
