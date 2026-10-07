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
        var worksheet = await Task.FromResult(new WorksheetWidgetViewModel()
        {
            Worksheet = worksheetDto,
            IconMap = new Dictionary<string, string>()
            {
                { "String", "fa-solid fa-font" },
                { "Phone", "fa-solid fa-phone" },
                { "Date", "fa-regular fa-calendar-days" },
                { "Email", "fa-regular fa-envelope" },
                { "Radio", "fa-regular fa-circle-dot" },
                { "Checkbox", "fa-regular fa-square-check" },
                { "CheckboxGroup", "fa-solid fa-list-check" },
                { "SelectList", "fa-solid fa-bars-staggered" },
                { "BCAddress", "fa-solid fa-globe" },
                { "TextArea", "fa-solid fa-paragraph" },
                { "Text", "fa-solid fa-font" },
                { "Currency", "custom-icon-text custom-dollar" },
                { "YesNo", "custom-icon-text custom-yesno" },
                { "Numeric", "custom-icon-text custom-numeric" },
                { "DataGrid", "fa-solid fa-table-cells" }
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
        context.Files
          .AddIfNotContains("/Pages/WorksheetConfiguration/UpsertCustomFieldModal.js");
    }
}