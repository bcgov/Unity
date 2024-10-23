using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget
{
    [ViewComponent(Name = "DataGridDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/DataGridDefinition/Refresh",
        ScriptTypes = [typeof(DataGridDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(DataGridDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class DataGridDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return null;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            await Task.CompletedTask;
            return View(new DataGridDefinitionViewModel());
        }
    }

    public class DataGridDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridDefinitionWidget/Default.css");
        }
    }

    public class DataGridDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridDefinitionWidget/Default.js");
        }
    }
}
