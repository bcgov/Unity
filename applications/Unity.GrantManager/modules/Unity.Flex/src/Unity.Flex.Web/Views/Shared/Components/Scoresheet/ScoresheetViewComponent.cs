using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.Scoresheet;

[Widget(
    RefreshUrl = "Widget/Scoresheet/Refresh",
    ScriptTypes = [typeof(ScoresheetWidgetScriptBundleContributor)],
    StyleTypes = [typeof(ScoresheetWidgetStyleBundleContributor)],
    AutoInitialize = true)]
public class ScoresheetViewComponent : AbpViewComponent
{
    private readonly IScoresheetAppService _scoresheetService;
    public ScoresheetViewComponent(IScoresheetAppService scoresheetService)
    {
        _scoresheetService = scoresheetService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var scoresheets = await _scoresheetService.GetListAsync();
        return View(new ScoresheetViewModel() { Scoresheets = scoresheets });
    }
}

public class ScoresheetWidgetStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/Scoresheet/Scoresheet.css");
    }
}

public class ScoresheetWidgetScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/Scoresheet/Scoresheet.js");
        context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresWidget/Default.js");
    }
}