using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetConfiguration;

[Widget(
    ScriptTypes = [typeof(WorksheetConfigurationScriptBundleContributor)],
    StyleTypes = [typeof(WorksheetConfigurationStyleBundleContributor)],
    AutoInitialize = true)]
public class WorksheetConfigurationViewComponent(IConfiguration configuration) : AbpViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View(new WorksheetConfigurationViewModel
        {
            MaxFileSize = configuration["S3:MaxFileSize"] ?? ""
        });
    }
}

public class WorksheetConfigurationViewModel
{
    public string MaxFileSize { get; set; } = "";
}

public class WorksheetConfigurationStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Pages/WorksheetConfiguration/Index.css");
    }
}

public class WorksheetConfigurationScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/libs/sortablejs/Sortable.js");
        context.Files.AddIfNotContains("/Pages/WorksheetConfiguration/Index.js");
    }
}
