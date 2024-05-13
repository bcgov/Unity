using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[ViewComponent(Name = "WorksheetInstanceWidget")]
[Widget(
        RefreshUrl = "Widgets/WorksheetInstance/Refresh",
        ScriptTypes = [typeof(WorksheetInstanceWidgetScriptBundleContributor)],
        StyleTypes = [typeof(WorksheetInstanceWidgetStyleBundleContributor)],
        AutoInitialize = true)]
public class WorksheetInstanceWidget(IWorksheetInstanceAppService worksheetInstanceAppService) : AbpViewComponent
{
    private readonly IWorksheetInstanceAppService _worksheetInstanceAppService = worksheetInstanceAppService;

    public async Task<IViewComponentResult> InvokeAsync(Guid correlationId, string correlationProvider, string uiAnchor)
    {
        var worksheetInstance = await _worksheetInstanceAppService.GetByCorrelationAsync(correlationId, correlationProvider, uiAnchor);       

        var viewModel = new WorksheetInstanceWidgetViewModel()
        {
        };        

        return View(viewModel);
    }

    public class WorksheetInstanceWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/WorksheetInstanceWidget/Default.css");
        }
    }

    public class WorksheetInstanceWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/WorksheetInstanceWidget/Default.js");
        }
    }
}

