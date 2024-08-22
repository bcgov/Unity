using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.SelectListWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/SelectList/Refresh",
        ScriptTypes = [typeof(SelectListWidgetScriptBundleContributor)],
        StyleTypes = [typeof(SelectListWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class SelectListWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new SelectListViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class SelectListWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SelectListWidget/Default.css");
        }
    }

    public class SelectListWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SelectListWidget/Default.js");
        }
    }
}
