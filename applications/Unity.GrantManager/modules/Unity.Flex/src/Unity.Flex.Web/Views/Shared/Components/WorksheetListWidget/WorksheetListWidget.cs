using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Flex.Worksheets;
using System.Linq;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetList;

[Widget(
    RefreshUrl = "Widget/WorksheetList/Refresh",
    ScriptTypes = [typeof(WorksheetListWidgetScriptBundleContributor)],
    StyleTypes = [typeof(WorksheetListWidgetStyleBundleContributor)],
    AutoInitialize = true)]
public class WorksheetListWidget : AbpViewComponent
{
    private readonly IWorksheetAppService _worksheetAppService;
    public WorksheetListWidget(IWorksheetAppService worksheetAppService)
    {
        _worksheetAppService = worksheetAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var worksheets = (await _worksheetAppService.GetListAsync()).OrderBy(s => s.Title);
        return View(new WorksheetListViewModel() { Worksheets = [.. worksheets] });
    }
}

public class WorksheetListWidgetStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/WorksheetListWidget/WorksheetList.css");
    }
}

public class WorksheetListWidgetScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/WorksheetListWidget/WorksheetList.js");
    }
}