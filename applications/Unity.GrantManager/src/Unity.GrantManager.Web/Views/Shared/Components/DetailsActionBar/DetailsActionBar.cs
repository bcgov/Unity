using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;

[Widget(
    ScriptFiles = new[] { "/Views/Shared/Components/DetailsActionBar/Default.js" },
    StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" },
    AutoInitialize = true,
    RefreshUrl = "Widgets/DetailsActionBar/Refresh"
)]
public class DetailsActionBar : AbpViewComponent
{
    [TempData]
    public string SelectedApplicationId { get; set; } = "";

    private readonly GrantApplicationAppService _applicationAppService;

    public DetailsActionBar(GrantApplicationAppService applicationAppService)
    {
        _applicationAppService = applicationAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        SelectedApplicationId = applicationId.ToString();

        var viewModel = new DetailsActionBarViewModel()
        {
            ApplicationId = applicationId,
            ApplicationActions = await _applicationAppService.GetActions(applicationId)
        };

        return View(viewModel);
    }
}

