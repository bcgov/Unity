using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationActionWidget;

[ViewComponent(Name = "ApplicationActionWidget")]
[Widget(
    ScriptFiles = new[] { "/Views/Shared/Components/ApplicationActionWidget/Default.js" },
    StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" },
    RefreshUrl = "Widgets/ApplicationActionWidget/Refresh",
    AutoInitialize = true
)]
public class ApplicationActionWidget : AbpViewComponent
{
    private readonly GrantApplicationAppService _applicationAppService;

    public ApplicationActionWidget(GrantApplicationAppService applicationAppService)
    {
        _applicationAppService = applicationAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        var viewModel = new ApplicationActionWidgetViewModel()
        {
            ApplicationId = applicationId,
            ApplicationActions = await _applicationAppService.GetActions(applicationId)
        };

        return View(viewModel);
    }
}

