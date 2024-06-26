using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets;

[Widget(
    RefreshUrl = "Widget/LinkWorksheet/Refresh",
    ScriptTypes = [typeof(LinkWorksheetWidgetScriptBundleContributor)],
    StyleTypes = [typeof(LinkWorksheetWidgetStyleBundleContributor)],
    AutoInitialize = true)]
public class LinkWorksheetWidget : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid worksheetId)
    {
        var worksheet = await Task.FromResult(new LinkWorksheetWidgetViewModel() {  });
        return View(worksheet);
    }
}

public class LinkWorksheetWidgetStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/LinkWorksheetWidget/LinkWorksheet.css");
    }
}

public class LinkWorksheetWidgetScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/LinkWorksheetWidget/LinkWorksheet.js");
    }
}