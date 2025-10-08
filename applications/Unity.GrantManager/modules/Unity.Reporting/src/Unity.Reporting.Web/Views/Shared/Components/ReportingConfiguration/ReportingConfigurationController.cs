using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("ReportingConfiguration")]
    public class ReportingConfigurationController : AbpController
    {
        private readonly IReportMappingService _reportMappingService;

        public ReportingConfigurationController(IReportMappingService reportMappingService)
        {
            _reportMappingService = reportMappingService;
        }

        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid formId, Guid? selectedVersionId = null)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for ReportingConfigurationController:Refresh");
                return ViewComponent(typeof(ReportingConfigurationViewComponent));
            }
            
            return ViewComponent(typeof(ReportingConfigurationViewComponent), new { formId, selectedVersionId });
        }

        [HttpGet]
        [Route("GetConfiguration")]
        public async Task<IActionResult> GetConfiguration(Guid correlationId, string correlationProvider)
        {
            try
            {
                var result = await _reportMappingService.GetByCorrelationAsync(correlationId, correlationProvider);
                return Ok(result);
            }
            catch (Volo.Abp.Domain.Entities.EntityNotFoundException)
            {
                // Return 404 when no mapping exists
                return NotFound();
            }
        }

        [HttpGet]
        [Route("Exists")]
        public async Task<IActionResult> Exists(Guid correlationId, string correlationProvider)
        {
            try
            {
                var exists = await _reportMappingService.ExistsAsync(correlationId, correlationProvider);
                return Ok(exists);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetFieldsMetadata")]
        public async Task<IActionResult> GetFieldsMetadata(Guid correlationId, string correlationProvider)
        {
            try
            {
                var result = await _reportMappingService.GetFieldsMetadataAsync(correlationId, correlationProvider);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] UpsertReportColumnsMapDto configuration)
        {
            try
            {
                var result = await _reportMappingService.CreateAsync(configuration);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("Update")]
        public async Task<IActionResult> Update([FromBody] UpsertReportColumnsMapDto configuration)
        {
            try
            {
                var result = await _reportMappingService.UpdateAsync(configuration);
                return Ok(result);
            }
            catch (Volo.Abp.Domain.Entities.EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("IsViewNameAvailable")]
        public async Task<IActionResult> IsViewNameAvailable(string viewName, Guid? correlationId = null, string? correlationProvider = null)
        {
            try
            {
                bool isAvailable;
                
                if (correlationId.HasValue && !string.IsNullOrWhiteSpace(correlationProvider))
                {
                    // Use correlation-aware availability check
                    isAvailable = await _reportMappingService.IsViewNameAvailableAsync(viewName, correlationId.Value, correlationProvider);
                }
                else
                {
                    // Use basic availability check (backward compatibility)
                    isAvailable = await _reportMappingService.IsViewNameAvailableAsync(viewName);
                }
                
                return Ok(isAvailable);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GenerateView")]
        public async Task<IActionResult> GenerateView([FromBody] GenerateViewRequest request)
        {
            try
            {
                var result = await _reportMappingService.GenerateViewAsync(
                    request.CorrelationId, 
                    request.CorrelationProvider, 
                    request.ViewName);
                
                // Return 202 Accepted for async operations that have been queued
                return Accepted(result);
            }
            catch (Volo.Abp.Domain.Entities.EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            { 
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GenerateColumnNames")]
        public IActionResult GenerateColumnNames([FromBody] GenerateColumnNamesRequest request)
        {
            try
            {
                var result = _reportMappingService.GenerateColumnNames(request.PathColumns);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetViewPreviewData")]
        public async Task<IActionResult> GetViewPreviewData(string viewName, int skip = 0, int take = 100, string? filter = null, string? orderBy = null)
        {
            try
            {
                var request = new Unity.Reporting.Configuration.ViewDataRequest
                {
                    Skip = skip,
                    Take = take,
                    Filter = filter,
                    OrderBy = orderBy
                };

                var result = await _reportMappingService.GetViewPreviewDataAsync(viewName, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("Delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteViewRequest deleteViewRequest)
        {
            try
            {
                await _reportMappingService.DeleteAsync(deleteViewRequest.CorrelationId, deleteViewRequest.CorrelationProvider, deleteViewRequest.DeleteView);
                return Ok(new { message = "Configuration and view deleted successfully" });
            }
            catch (Volo.Abp.Domain.Entities.EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class GenerateViewRequest
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
    }

    public class GenerateColumnNamesRequest
    {
        public Dictionary<string, string> PathColumns { get; set; } = new Dictionary<string, string>();
    }

    public class ViewDataRequest
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public string? Filter { get; set; }
        public string? OrderBy { get; set; }
    }

    public class DeleteViewRequest
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public bool DeleteView { get; set; } = true;
    }
}
