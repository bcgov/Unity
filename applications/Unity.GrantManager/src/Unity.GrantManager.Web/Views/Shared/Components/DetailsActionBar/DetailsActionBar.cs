using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;

[Widget(ScriptFiles = new[] { "/Views/Shared/Components/DetailsActionBar/Default.js" , "/Pages/ApplicationTags/ApplicationTags.js" },
    StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" })]
public class DetailsActionBar(
    IGrantApplicationAppService grantApplicationAppService,
    IAuthorizationService authorizationService) : AbpViewComponent
{
    [BindProperty]
    public Guid SelectedApplicationId { get; set; }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        SelectedApplicationId = applicationId;

        var application = await grantApplicationAppService.GetBasicAsync(SelectedApplicationId);
        var canUpdateExternalStatusVisibility = await authorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.Default);

        return View(new DetailsActionBarViewModel
        {
            ApplicationId = SelectedApplicationId,
            ExternalStatusVisibility = application.ExternalStatusVisibility,
            CanUpdateExternalStatusVisibility = canUpdateExternalStatusVisibility
        });
    }
}