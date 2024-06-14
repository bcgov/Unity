using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    [Widget(
        RefreshUrl = "Widgets/CustomTab/RefreshCustomTab",
        ScriptTypes = [typeof(CustomTabWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CustomTabWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CustomTabWidgetViewComponent : AbpViewComponent
    {
        public CustomTabWidgetViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid instanceCorrelationId, 
            string instanceCorrelationProvider, 
            Guid sheetCorrelationId,
            string sheetCorrelationProvider,
            string uiAnchor,
            string name,
            string title)
        {
            CustomTabWidgetViewModel model = new()
            {
                InstanceCorrelationId = instanceCorrelationId,
                InstanceCorrelationProvider = instanceCorrelationProvider,
                SheetCorrelationId = sheetCorrelationId,
                SheetCorrelationProvider = sheetCorrelationProvider,
                UiAnchor = uiAnchor,
                Name = name,
                Title = title
            };

            await Task.CompletedTask;

            return View(model);
        }
    }

    public class CustomTabWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomTabWidget/Default.css");
        }
    }

    public class CustomTabWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomTabWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
