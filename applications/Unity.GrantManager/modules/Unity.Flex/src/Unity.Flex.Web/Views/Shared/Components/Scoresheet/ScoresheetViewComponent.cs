using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Views.Shared.Components.Scoresheet;

[Widget(
    RefreshUrl = "Widget/Scoresheet/Refresh",
    AutoInitialize = true)]
public class ScoresheetViewComponent : AbpViewComponent
{
    private readonly IScoresheetAppService _scoresheetService;
    public ScoresheetViewComponent(IScoresheetAppService scoresheetService)
    {
        _scoresheetService = scoresheetService;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
    {
        var scoresheets = await _scoresheetService.GetAllAsync();
        return View(new ScoresheetViewModel() { Scoresheets = scoresheets });
    }
}

