using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus
{
    [Widget(
        RefreshUrl = "ReportingConfigurationViewStatus/Refresh",
        ScriptTypes = new[] { typeof(ReportingConfigurationViewStatusScriptBundleContributor) },
        StyleTypes = new[] { typeof(ReportingConfigurationViewStatusStyleBundleContributor) },
        AutoInitialize = true)]
    public class ReportingConfigurationViewStatusViewComponent : AbpViewComponent
    {
        private readonly IReportMappingService _reportMappingService;

        public ReportingConfigurationViewStatusViewComponent(IReportMappingService reportMappingService)
        {
            _reportMappingService = reportMappingService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid versionId, string provider)
        {
            var model = new ReportingConfigurationViewStatusViewModel
            {
                VersionId = versionId,
                Provider = provider
            };

            try 
            {
                var exists = await _reportMappingService.ExistsAsync(versionId, provider);
                
                if (exists)
                {
                    var reportColumnsMapViewStatus = await _reportMappingService.GetViewStatusByCorrlationAsync(versionId, provider);
                    model.ViewName = reportColumnsMapViewStatus.ViewName;
                    model.ViewStatus = reportColumnsMapViewStatus.ViewStatus;
                    model.HasMapping = true;
                }
            }
            catch
            {
                // If there's an error, we'll show the component with no status
                model.HasMapping = false;
            }

            return View(model);
        }
    }

    public class ReportingConfigurationViewStatusStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ReportingConfigurationViewStatus/Default.css");
        }
    }

    public class ReportingConfigurationViewStatusScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ReportingConfigurationViewStatus/Default.js");
        }
    }
}