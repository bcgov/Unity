using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Views.Shared.Components.ReviewList
{
    [Widget(
        ScriptFiles = new[]
        {
            "/Views/Shared/Components/ReviewList/ReviewList.js"
        },
        StyleFiles = new[]
        {
            "/Views/Shared/Components/ReviewList/ReviewList.css"
        })]
    public class ReviewList(
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var scoringFeatureEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");
            ViewBag.IsAIScoringEnabled =
                scoringFeatureEnabled &&
                await permissionChecker.IsGrantedAsync(AIPermissions.ScoringAssistant.ScoringAssistantDefault);

            return View();
        }
    }
}
