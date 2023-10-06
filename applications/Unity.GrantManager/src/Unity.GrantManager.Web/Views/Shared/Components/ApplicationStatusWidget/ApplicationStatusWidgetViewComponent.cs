using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationStatusWidget
{
    [Widget(
        RefreshUrl = "Widgets/Status/RefreshStatus",
        ScriptTypes = new[] { typeof(ApplicationStatusWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationStatusWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationStatusWidgetViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _applicationAppService;

        public ApplicationStatusWidgetViewComponent(IGrantApplicationAppService applicationAppService)
        {
            _applicationAppService = applicationAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicationStatus = await _applicationAppService.GetApplicationStatusAsync(applicationId);
            return View(new ApplicationStatusWidgetViewModel() { ApplicationStatus = applicationStatus.InternalStatus });
        }
    }

    public class ApplicationStatusWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationStatusWidget/Default.css");
        }
    }

    public class ApplicationStatusWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationStatusWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
