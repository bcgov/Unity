using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.CurrencyWidget
{
    [ViewComponent(Name = "CurrencyWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/Currency/Refresh",
        ScriptTypes = [typeof(CurrencyWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CurrencyWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CurrencyWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new CurrencyViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class CurrencyWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CurrencyWidget/Default.css");
        }
    }

    public class CurrencyWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CurrencyWidget/Default.js");
        }
    }
}
