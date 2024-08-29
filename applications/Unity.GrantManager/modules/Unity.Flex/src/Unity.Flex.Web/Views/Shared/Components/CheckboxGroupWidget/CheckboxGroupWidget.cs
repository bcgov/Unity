using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupWidget
{
    [ViewComponent(Name = "CheckboxGroupWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/CheckboxGroup/Refresh",
        ScriptTypes = [typeof(CheckboxGroupWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CheckboxGroupWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CheckboxGroupWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new CheckboxGroupViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class CheckboxGroupWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxGroupWidget/Default.css");
        }
    }

    public class CheckboxGroupWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxGroupWidget/Default.js");
        }
    }
}
