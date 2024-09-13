using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets;

[Widget(
    RefreshUrl = "../Flex/Widgets/Worksheet/Refresh",
    ScriptTypes = [typeof(WorksheetWidgetScriptBundleContributor)],
    StyleTypes = [typeof(WorksheetWidgetStyleBundleContributor)],
    AutoInitialize = true)]
public class WorksheetWidget : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(WorksheetDto worksheetDto)
    {
        var worksheet = await Task.FromResult(new WorksheetWidgetViewModel() { Worksheet = worksheetDto });
        return View(worksheet);
    }
}

public class WorksheetWidgetStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/WorksheetWidget/Worksheet.css");
        context.Files
          .AddIfNotContains("/Views/Shared/Components/Scoresheet/Scoresheet.css");
    }
}

public class WorksheetWidgetScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/WorksheetWidget/Worksheet.js");
    }
}