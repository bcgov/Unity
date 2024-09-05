using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxWidget
{
    [ViewComponent(Name = "CheckboxWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/Checkbox/Refresh",
        ScriptTypes = [typeof(CheckboxWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CheckboxWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CheckboxWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new CheckboxViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class CheckboxWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxWidget/Default.css");
        }
    }

    public class CheckboxWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxWidget/Default.js");
        }
    }
}
