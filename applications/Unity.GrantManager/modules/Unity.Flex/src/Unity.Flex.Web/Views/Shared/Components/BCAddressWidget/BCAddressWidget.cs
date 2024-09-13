using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.BCAddressWidget
{
    [ViewComponent(Name = "BCAddressWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/BCAddress/Refresh",
        ScriptTypes = [typeof(BCAddressWidgetScriptBundleContributor)],
        StyleTypes = [typeof(BCAddressWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class BCAddressWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {            
            return View(await Task.FromResult(new BCAddressViewModel()
            {                
                Field = fieldModel,
                Name = modelName
            }));
        }
    }

    public class BCAddressWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/BCAddressWidget/Default.css");
        }
    }

    public class BCAddressWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/BCAddressWidget/Default.js");
        }
    }
}
