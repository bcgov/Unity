using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus
{
    /// <summary>
    /// ASP.NET Core MVC controller for the ReportingConfigurationViewStatus view component providing AJAX endpoints.
    /// Handles widget refresh operations and view data preview functionality for the reporting configuration status display.
    /// Supports dynamic status updates through AJAX calls and provides preview data access for generated reporting views
    /// with proper error handling and JSON response formatting for client-side consumption.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the ReportingConfigurationViewStatusController with required dependencies.
    /// Sets up the report mapping service for accessing view status and preview data functionality.
    /// </remarks>
    /// <param name="reportMappingService">The service for managing report mappings and view operations.</param>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("ApplicationForms/ReportingConfigurationViewStatus")]
    public class ReportingConfigurationViewStatusController(IReportMappingService reportMappingService) : AbpController
    {
        /// <summary>
        /// Handles AJAX requests to refresh the ReportingConfigurationViewStatus view component with updated status information.
        /// Validates the request parameters and returns the refreshed view component for dynamic status updates
        /// without requiring a full page reload. Used for real-time status monitoring during view generation.
        /// </summary>
        /// <param name="versionId">The correlation version identifier for the mapping configuration.</param>
        /// <param name="provider">The correlation provider type (e.g., "formversion", "scoresheet").</param>
        /// <returns>A ViewComponent result containing the updated status display or a default component on validation failure.</returns>
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid versionId, string provider)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for ReportingConfigurationViewStatusController: RefreshViewStatus");
                return ViewComponent("ReportingConfigurationViewStatus");
            }
            
            return ViewComponent("ReportingConfigurationViewStatus", new { versionId, provider });
        }

        /// <summary>
        /// Handles AJAX requests to retrieve preview data from generated reporting views for display in modal dialogs.
        /// Fetches sample data from the top application record in the view and returns formatted JSON response
        /// with view metadata including column names and record counts. Provides error handling with user-friendly
        /// messages for various failure scenarios such as missing views or data access issues.
        /// </summary>
        /// <param name="versionId">The correlation version identifier for locating the associated view mapping.</param>
        /// <param name="provider">The correlation provider type that determines the mapping lookup strategy.</param>
        /// <returns>A JSON result containing preview data with success status, view information, and sample records, or error details on failure.</returns>
        [HttpGet]
        [Route("PreviewData")]
        public async Task<IActionResult> PreviewData(Guid versionId, string provider)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for ReportingConfigurationViewStatusController: PreviewData");
                return ViewComponent("ReportingConfigurationViewStatus");
            }

            try
            {
                var reportColumnsMap = await reportMappingService.GetByCorrelationAsync(versionId, provider);
                
                if (reportColumnsMap?.ViewName == null)
                {
                    return Json(new { success = false, message = "View not found or not generated yet." });
                }

                // Get preview data (top 1 record based on ApplicationId)
                var request = new ViewDataRequest
                {
                    Skip = 0,
                    Take = 100, // This will be ignored by the preview method since it uses LIMIT 1 pattern
                    OrderBy = null,
                    Filter = null
                };

                var viewData = await reportMappingService.GetViewPreviewDataAsync(reportColumnsMap.ViewName, request);
                
                return Json(new { 
                    success = true, 
                    viewName = reportColumnsMap.ViewName,
                    data = viewData.Data,
                    columns = viewData.ColumnNames,
                    totalCount = viewData.TotalCount
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving view preview data");
                return Json(new { success = false, message = "Error retrieving view preview data: " + ex.Message });
            }
        }
    }
}