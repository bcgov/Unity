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
    /// <summary>
    /// ASP.NET Core view component for displaying compact status information about reporting view generation and mapping configuration.
    /// Provides real-time status updates, auto-refresh capabilities, and interactive preview functionality for generated database views.
    /// Integrates with background job processing to show current generation status and enables users to preview data
    /// from successfully generated reporting views with proper error handling and user feedback.
    /// </summary>
    [Widget(
        RefreshUrl = "ReportingConfigurationViewStatus/Refresh",
        ScriptTypes = new[] { typeof(ReportingConfigurationViewStatusScriptBundleContributor) },
        StyleTypes = new[] { typeof(ReportingConfigurationViewStatusStyleBundleContributor) },
        AutoInitialize = true)]
    public class ReportingConfigurationViewStatusViewComponent : AbpViewComponent
    {
        private readonly IReportMappingService _reportMappingService;

        /// <summary>
        /// Initializes a new instance of the ReportingConfigurationViewStatusViewComponent with required dependencies.
        /// Sets up the report mapping service for accessing mapping configuration and view status information.
        /// </summary>
        /// <param name="reportMappingService">The service for managing report mappings and view status operations.</param>
        public ReportingConfigurationViewStatusViewComponent(IReportMappingService reportMappingService)
        {
            _reportMappingService = reportMappingService;
        }

        /// <summary>
        /// Renders the view component with current mapping status and view generation information for the specified correlation.
        /// Attempts to retrieve existing mapping configuration and view status, gracefully handling errors by displaying
        /// the component in a "no mapping" state. Populates the view model with correlation details and status information
        /// for proper rendering of status indicators and action buttons.
        /// </summary>
        /// <param name="versionId">The correlation version identifier for retrieving mapping and view status information.</param>
        /// <param name="provider">The correlation provider type (e.g., "formversion", "scoresheet") that determines lookup strategy.</param>
        /// <returns>A view component result containing the rendered status display with current mapping and view information.</returns>
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

    /// <summary>
    /// Bundle contributor for CSS styles specific to the ReportingConfigurationViewStatus view component.
    /// Ensures the component's stylesheet is included in the page bundle for proper visual rendering
    /// of status indicators, buttons, and layout elements with conditional inclusion to avoid duplicates.
    /// </summary>
    public class ReportingConfigurationViewStatusStyleBundleContributor : BundleContributor
    {
        /// <summary>
        /// Configures the CSS bundle by adding the view component's stylesheet with conditional inclusion.
        /// Prevents duplicate CSS file inclusion while ensuring the component's styles are available
        /// for proper rendering of status indicators and interactive elements.
        /// </summary>
        /// <param name="context">The bundle configuration context for adding CSS files to the page bundle.</param>
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ReportingConfigurationViewStatus/Default.css");
        }
    }

    /// <summary>
    /// Bundle contributor for JavaScript files required by the ReportingConfigurationViewStatus view component.
    /// Ensures required JavaScript libraries and component-specific scripts are included in the page bundle
    /// for proper functionality including PubSub messaging, auto-refresh, and preview capabilities.
    /// </summary>
    public class ReportingConfigurationViewStatusScriptBundleContributor : BundleContributor
    {
        /// <summary>
        /// Configures the JavaScript bundle by adding required libraries and component scripts with conditional inclusion.
        /// Includes PubSub library for inter-component communication and the component's main JavaScript file
        /// for status monitoring, auto-refresh, and preview functionality without duplicate inclusions.
        /// </summary>
        /// <param name="context">The bundle configuration context for adding JavaScript files to the page bundle.</param>
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ReportingConfigurationViewStatus/Default.js");
        }
    }
}