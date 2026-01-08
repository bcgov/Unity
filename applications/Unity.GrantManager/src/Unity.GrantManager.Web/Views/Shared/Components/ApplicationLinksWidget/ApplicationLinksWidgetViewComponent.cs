using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget
{
    [Widget(
        RefreshUrl = "Widgets/ApplicationLinks/RefreshApplicationLinks",
        ScriptTypes = new[] { typeof(ApplicationLinksWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationLinksWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationLinksWidgetViewComponent : AbpViewComponent
    {
        public IViewComponentResult Invoke(Guid applicationId)
        {
            // DataTables will load the data via AJAX, so we don't need to pre-load it here
            ApplicationLinksWidgetViewModel model = new() {
                ApplicationLinks = [], // Empty list since DataTables will load the data
                ApplicationId = applicationId
            };

            return View(model);
        }
    }

    public class ApplicationLinksWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationLinksWidget/Default.css");
        }
    }

    public class ApplicationLinksWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationLinksWidget/Default.js");
            context.Files
              .AddIfNotContains("/Pages/ApplicationLinks/ApplicationLinks.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
