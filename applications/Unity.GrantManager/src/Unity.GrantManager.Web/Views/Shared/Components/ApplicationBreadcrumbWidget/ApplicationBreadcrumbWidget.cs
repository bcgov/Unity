using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    [Widget(
        RefreshUrl = "Widgets/ApplicationBreadcrumb/RefreshApplicationBreadcrumb",        
        ScriptTypes = new[] { typeof(ApplicationBreadcrumbWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationBreadcrumbWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationBreadcrumbWidgetViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _applicationAppService;

         public ApplicationBreadcrumbWidgetViewComponent(IGrantApplicationAppService applicationAppService)
        {
            _applicationAppService = applicationAppService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            GrantApplicationDto application = await _applicationAppService.GetAsync(applicationId);
            return View(new ApplicationBreadcrumbWidgetViewModel() { GrantApplication = application });
        }
    }

    public class ApplicationBreadcrumbWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationBreadcrumbWidget/Default.css");
        }
    }

    public class ApplicationBreadcrumbWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationBreadcrumbWidget/Default.js");
        }
    }
}

