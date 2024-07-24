using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.DateWidget
{
    [ViewComponent(Name = "DateWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/Date/Refresh",
        ScriptTypes = [typeof(DateWidgetScriptBundleContributor)],
        StyleTypes = [typeof(DateWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class DateWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new DateViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class DateWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DateWidget/Default.css");
        }
    }

    public class DateWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DateWidget/Default.js");
        }
    }
}
