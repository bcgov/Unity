using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget
{
    [Widget(
        RefreshUrl = "Widgets/ApplicationLinks/RefreshApplicationLinks",
        ScriptTypes = new[] { typeof(ApplicationLinksWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationLinksWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationLinksWidgetViewComponent : AbpViewComponent
    {
        private readonly IApplicationLinksService _applicationLinksService;

        public ApplicationLinksWidgetViewComponent(IApplicationLinksService applicationLinksService)
        {
            _applicationLinksService = applicationLinksService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            List<ApplicationLinksInfoDto> applicationLinks = await _applicationLinksService.GetListByApplicationAsync(applicationId);
            ApplicationLinksWidgetViewModel model = new() {
                ApplicationLinks = applicationLinks,
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
