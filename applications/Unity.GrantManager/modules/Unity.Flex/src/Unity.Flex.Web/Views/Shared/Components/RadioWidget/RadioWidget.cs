using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.RadioWidget
{
    [ViewComponent(Name = "RadioWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/Radio/Refresh",
        ScriptTypes = [typeof(RadioWidgetScriptBundleContributor)],
        StyleTypes = [typeof(RadioWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class RadioWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new RadioViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class RadioWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/RadioWidget/Default.css");
        }
    }

    public class RadioWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/RadioWidget/Default.js");
        }
    }
}
