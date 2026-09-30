using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Permissions;
using Unity.Modules.Shared.Specializations;
using Unity.TenantManagement;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;

[Widget(ScriptFiles = new[] { "/Views/Shared/Components/DetailsActionBar/Default.js" , "/Pages/ApplicationTags/ApplicationTags.js" },
    StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" })]
public class DetailsActionBar(
    IGrantApplicationAppService grantApplicationAppService,
    IAuthorizationService authorizationService,
    IFeatureChecker featureChecker,
    IOnboardingRequestAppService onboardingRequestAppService) : AbpViewComponent
{
    [BindProperty]
    public Guid SelectedApplicationId { get; set; }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        SelectedApplicationId = applicationId;

        var application = await grantApplicationAppService.GetBasicAsync(SelectedApplicationId);
        var canPublishStatus = await authorizationService.IsGrantedAnyAsync(UnitySelector.Application.Status.Publish);
        var canUnpublishStatus = await authorizationService.IsGrantedAnyAsync(UnitySelector.Application.Status.Unpublish);

        var isOnboardingRequest = await featureChecker.IsEnabledAsync(SpecializationConsts.Onboarding)
            && (await authorizationService.AuthorizeAsync(HttpContext.User, IdentityConsts.ITOperationsPolicyName)).Succeeded;

        return View(new DetailsActionBarViewModel
        {
            ApplicationId = SelectedApplicationId,
            ExternalStatusVisibility = application.ExternalStatusVisibility,
            CanUpdateExternalStatusVisibility = (!application.ExternalStatusVisibility && canPublishStatus) || (application.ExternalStatusVisibility && canUnpublishStatus),
            IsOnboardingRequest = isOnboardingRequest,
            CanCreateTenant = isOnboardingRequest
                && (await onboardingRequestAppService.GetAsync(SelectedApplicationId))?.IsApproved == true
        });
    }
}
