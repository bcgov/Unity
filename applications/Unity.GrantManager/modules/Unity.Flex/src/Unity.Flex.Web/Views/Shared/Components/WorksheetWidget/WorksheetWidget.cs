﻿using Microsoft.AspNetCore.Mvc;
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
        var worksheet = await Task.FromResult(new WorksheetWidgetViewModel()
        {
            Worksheet = worksheetDto,
            IconMap = new Dictionary<string, string>()
            {
                { "String", "fl fl-font" },
                { "Phone", "fl fl-phone" },
                { "Date", "fl fl-datetime" },
                { "Email", "fl fl-mail" },
                { "Radio", "fl fl-radio" },
                { "Checkbox", "fl fl-checkbox-checked" },
                { "CheckboxGroup", "fl fl-multi-select" },
                { "SelectList", "fl fl-list" },
                { "BCAddress", "fl fl-globe" },
                { "TextArea", "fl fl-text-area" },
                { "Text", "fl fl-font" },
                { "Currency", "custom-icon-text custom-dollar" },
                { "YesNo", "custom-icon-text custom-yesno" },
                { "Numeric", "custom-icon-text custom-numeric" },
                { "DataGrid", "fl fl-datagrid" }
            }
        });
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