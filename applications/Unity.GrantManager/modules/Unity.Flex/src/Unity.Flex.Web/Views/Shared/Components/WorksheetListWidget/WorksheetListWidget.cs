using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Flex.Worksheets;
using System.Linq;
using Unity.Flex.Web.Views.Shared.Components.Worksheets;
using Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget;
using Unity.Flex.Web.Views.Shared.Components.SelectListDefinitionWidget;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetList;

[Widget(
    RefreshUrl = "../Flex/Widgets/WorksheetList/Refresh",
    ScriptTypes = [
        typeof(WorksheetListWidgetScriptBundleContributor),
        typeof(WorksheetWidgetScriptBundleContributor),
        typeof(CheckboxGroupDefinitionWidgetScriptBundleContributor),
        typeof(SelectListDefinitionWidgetScriptBundleContributor)
    ],
    StyleTypes = [
        typeof(WorksheetListWidgetStyleBundleContributor),
        typeof(WorksheetWidgetStyleBundleContributor),
        typeof(CheckboxGroupDefinitionWidgetStyleBundleContributor),
        typeof(SelectListDefinitionWidgetStyleBundleContributor)
    ],
    AutoInitialize = true)]
public class WorksheetListWidget(IWorksheetAppService worksheetAppService) : AbpViewComponent
{
    private readonly IWorksheetAppService _worksheetAppService = worksheetAppService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var worksheets = (await _worksheetAppService.GetListAsync()).OrderBy(s => s.Title).ThenBy(s => s.Version);
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
