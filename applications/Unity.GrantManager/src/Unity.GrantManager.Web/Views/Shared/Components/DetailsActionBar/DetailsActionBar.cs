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
public class DetailsActionBar : AbpViewComponent
{
    [BindProperty]
    public Guid SelectedApplicationId { get; set; }

    private readonly IGrantApplicationAppService _grantApplicationAppService;
    private readonly IAuthorizationService _authorizationService;

    public DetailsActionBar(
        IGrantApplicationAppService grantApplicationAppService,
        IAuthorizationService authorizationService)
    {
        _grantApplicationAppService = grantApplicationAppService;
        _authorizationService = authorizationService;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        SelectedApplicationId = applicationId;

        var application = await _grantApplicationAppService.GetAsync(applicationId);
        var canUpdateExternalStatusVisibility = await _authorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.Default);

        return View(new DetailsActionBarViewModel
        {
            ApplicationId = applicationId,
            ExternalStatusVisibility = application.ExternalStatusVisibility,
            CanUpdateExternalStatusVisibility = canUpdateExternalStatusVisibility
        });
    }
}