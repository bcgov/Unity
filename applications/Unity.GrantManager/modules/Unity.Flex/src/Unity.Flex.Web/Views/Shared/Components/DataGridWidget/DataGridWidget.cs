using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    [ViewComponent(Name = "DataGridWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/DataGrid/Refresh",
        ScriptTypes = [typeof(DataGridWidgetScriptBundleContributor)],
        StyleTypes = [typeof(DataGridWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class DataGridWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new DataGridViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class DataGridWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridWidget/Default.css");
        }
    }

    public class DataGridWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridWidget/Default.js");
        }
    }
}
