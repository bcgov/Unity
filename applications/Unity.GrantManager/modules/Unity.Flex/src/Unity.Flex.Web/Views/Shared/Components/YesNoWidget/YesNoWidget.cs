using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.YesNoWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/YesNo/Refresh",
        ScriptTypes = [typeof(YesNoWidgetScriptBundleContributor)],
        StyleTypes = [typeof(YesNoWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class YesNoWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new YesNoViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class YesNoWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/YesNoWidget/Default.css");
        }
    }

    public class YesNoWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/YesNoWidget/Default.js");
        }
    }
}
