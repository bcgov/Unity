using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.Summary
{
    [Widget(
        RefreshUrl = "Widgets/Summary/RefreshSummary",
        ScriptTypes = new[] { typeof(SummaryWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(SummaryWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class SummaryWidgetViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _grantApplicationService;
        public SummaryWidgetViewComponent(IGrantApplicationAppService grantApplicationAppService)
        {
            _grantApplicationService = grantApplicationAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Boolean isReadOnly)
        {
            if (applicationId == Guid.Empty)
            {
                return View(new SummaryWidgetViewModel());
            }

            var summaryDto = await _grantApplicationService.GetSummaryAsync(applicationId);

            SummaryWidgetViewModel summaryWidgetViewModel = ObjectMapper.Map<GetSummaryDto, SummaryWidgetViewModel>(summaryDto);
            summaryWidgetViewModel.ApplicationId = applicationId;
            summaryWidgetViewModel.IsReadOnly = isReadOnly;

            return View(summaryWidgetViewModel);
        }
    }

    public class SummaryWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SummaryWidget/Default.css");
        }
    }

    public class SummaryWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SummaryWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
