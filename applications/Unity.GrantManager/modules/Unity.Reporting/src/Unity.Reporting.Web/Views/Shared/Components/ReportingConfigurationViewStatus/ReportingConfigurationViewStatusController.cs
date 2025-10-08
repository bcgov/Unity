using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("ApplicationForms/ReportingConfigurationViewStatus")]
    public class ReportingConfigurationViewStatusController : AbpController
    {
        private readonly IReportMappingService _reportMappingService;

        public ReportingConfigurationViewStatusController(IReportMappingService reportMappingService)
        {
            _reportMappingService = reportMappingService;
        }

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

        [HttpGet]
        [Route("PreviewData")]
        public async Task<IActionResult> PreviewData(Guid versionId, string provider)
        {
            try
            {
                var reportColumnsMap = await _reportMappingService.GetByCorrelationAsync(versionId, provider);
                
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

                var viewData = await _reportMappingService.GetViewPreviewDataAsync(reportColumnsMap.ViewName, request);
                
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