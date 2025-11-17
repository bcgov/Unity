using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Unity.Reporting.Permissions;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    /// <summary>
    /// ASP.NET Core MVC controller for the ReportingConfiguration view component providing comprehensive AJAX API endpoints.
    /// Handles all reporting configuration operations including mapping CRUD operations, field metadata retrieval,
    /// view name validation, view generation, column name generation, view data access, and configuration deletion.
    /// Provides RESTful API endpoints for the dynamic reporting configuration interface with proper authorization,
    /// validation, and error handling for seamless client-side integration.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("ReportingConfiguration")]
    [Authorize(ReportingPermissions.Configuration.Default)]
    public class ReportingConfigurationController(IReportMappingService reportMappingService) : AbpController
    {
        /// <summary>
        /// Handles AJAX requests to refresh the ReportingConfiguration view component with updated parameter values.
        /// Validates request parameters and returns the refreshed view component for dynamic interface updates
        /// when form versions, providers, or other configuration parameters change without requiring page reload.
        /// </summary>
        /// <param name="formId">The form identifier associated with the reporting configuration.</param>
        /// <param name="selectedVersionId">The optional selected form version identifier for correlation operations.</param>
        /// <param name="provider">The optional correlation provider type (e.g., "formversion", "scoresheet").</param>
        /// <returns>A ViewComponent result containing the updated configuration interface or default component on validation failure.</returns>
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid formId, Guid? selectedVersionId = null, string? provider = null)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for ReportingConfigurationController:Refresh");
                return ViewComponent(typeof(ReportingConfigurationViewComponent));
            }
            
            return ViewComponent(typeof(ReportingConfigurationViewComponent), new { formId, selectedVersionId, provider });
        }

        /// <summary>
        /// Retrieves an existing report mapping configuration for a specific correlation with detailed field mappings and metadata.
        /// Returns the complete mapping configuration including field-to-column mappings, view status, and detected schema changes.
        /// Provides 404 response when no mapping exists for the specified correlation parameters.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity (form version, scoresheet, etc.).</param>
        /// <param name="correlationProvider">The provider type identifier that determines the correlation lookup strategy.</param>
        /// <returns>An OK result with the mapping configuration DTO, or NotFound if no mapping exists for the correlation.</returns>
        [HttpGet]
        [Route("GetConfiguration")]
        public async Task<IActionResult> GetConfiguration(Guid correlationId, string correlationProvider)
        {
            try
            {
                var result = await reportMappingService.GetByCorrelationAsync(correlationId, correlationProvider);
                return Ok(result);
            }
            catch (Volo.Abp.Domain.Entities.EntityNotFoundException)
            {
                // Return 404 when no mapping exists
                return NotFound();
            }
        }

        /// <summary>
        /// Checks whether a report mapping configuration exists for the specified correlation parameters.
        /// Provides a lightweight existence check without retrieving the full mapping data, useful for
        /// conditional UI rendering and determining whether to show configuration or setup interfaces.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity to check for existing mapping.</param>
        /// <param name="correlationProvider">The provider type identifier that determines the lookup strategy.</param>
        /// <returns>An OK result with boolean existence status, or BadRequest for invalid correlation providers.</returns>
        [HttpGet]
        [Route("Exists")]
        public async Task<IActionResult> Exists(Guid correlationId, string correlationProvider)
        {
            try
            {
                var exists = await reportMappingService.ExistsAsync(correlationId, correlationProvider);
                return Ok(exists);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves comprehensive field metadata for all fields within a correlated entity for mapping configuration.
        /// Delegates to the appropriate field provider based on correlation provider to extract field definitions,
        /// types, paths, labels, and hierarchical structure information from the source schema for dynamic mapping setup.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose fields should be analyzed.</param>
        /// <param name="correlationProvider">The provider type identifier that determines which field provider to use.</param>
        /// <returns>An OK result with comprehensive field metadata, or BadRequest for invalid correlation providers.</returns>
        [HttpGet]
        [Route("GetFieldsMetadata")]
        public async Task<IActionResult> GetFieldsMetadata(Guid correlationId, string correlationProvider)
        {
            try
            {
                var result = await reportMappingService.GetFieldsMetadataAsync(correlationId, correlationProvider);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Creates a new report mapping configuration with automatic field analysis and column name generation.
        /// Processes the provided mapping configuration, validates field uniqueness and column name compliance,
        /// and creates the complete mapping with auto-generated column names for unmapped fields.
        /// Requires Configuration.Update permissions for security.
        /// </summary>
        /// <param name="configuration">The report mapping creation request containing correlation details and optional custom field mappings.</param>
        /// <returns>An OK result with the created mapping configuration, or BadRequest for validation failures and invalid correlation providers.</returns>
        [HttpPost]
        [Route("Create")]
        [Authorize(ReportingPermissions.Configuration.Update)]
        public async Task<IActionResult> Create([FromBody] UpsertReportColumnsMapDto configuration)
        {
            try
            {
                var result = await reportMappingService.CreateAsync(configuration);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing report mapping configuration with new field metadata while preserving existing column names.
        /// Intelligently merges current field metadata with existing mappings, allowing user overrides to take precedence
        /// while automatically handling new fields and maintaining column name uniqueness across the mapping.
        /// Requires Configuration.Update permissions for security.
        /// </summary>
        /// <param name="configuration">The report mapping update request containing correlation details and updated field mapping data.</param>
        /// <returns>An OK result with the updated mapping configuration, NotFound if mapping doesn't exist, or BadRequest for validation failures.</returns>
        [HttpPut]
        [Route("Update")]
        [Authorize(ReportingPermissions.Configuration.Update)]
        public async Task<IActionResult> Update([FromBody] UpsertReportColumnsMapDto configuration)
        {
            try
            {
                var result = await reportMappingService.UpdateAsync(configuration);
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

        /// <summary>
        /// Validates whether a view name is available for use in the database, with optional correlation-aware checking.
        /// Supports both basic availability checking (view doesn't exist) and correlation-aware checking (allowing reuse
        /// within the same correlation). Provides flexible validation for view naming operations during configuration setup.
        /// </summary>
        /// <param name="viewName">The proposed view name to validate for availability in the Reporting schema.</param>
        /// <param name="correlationId">Optional correlation ID for correlation-aware availability checking.</param>
        /// <param name="correlationProvider">Optional correlation provider for correlation-aware availability checking.</param>
        /// <returns>An OK result with boolean availability status, or BadRequest for invalid view names or correlation parameters.</returns>
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
                    isAvailable = await reportMappingService.IsViewNameAvailableAsync(viewName, correlationId.Value, correlationProvider);
                }
                else
                {
                    // Use basic availability check (backward compatibility)
                    isAvailable = await reportMappingService.IsViewNameAvailableAsync(viewName);
                }
                
                return Ok(isAvailable);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Initiates asynchronous generation of a database view from an existing report mapping configuration.
        /// Validates view name availability, updates mapping status to generating, and queues a background job
        /// for actual view creation in the database. Returns 202 Accepted for proper async operation indication.
        /// Requires Configuration.Update permissions for security.
        /// </summary>
        /// <param name="request">The view generation request containing correlation details and desired view name.</param>
        /// <returns>An Accepted result with generation status information, NotFound if mapping doesn't exist, or BadRequest for validation failures.</returns>
        [HttpPost]
        [Route("GenerateView")]
        [Authorize(ReportingPermissions.Configuration.Update)]
        public async Task<IActionResult> GenerateView([FromBody] GenerateViewRequest request)
        {
            try
            {
                var result = await reportMappingService.GenerateViewAsync(
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

        /// <summary>
        /// Generates sanitized and unique PostgreSQL-compatible column names from field path-to-label mappings.
        /// Processes field labels through sanitization to remove invalid characters, enforces uniqueness with numeric suffixes,
        /// and ensures all names comply with PostgreSQL identifier restrictions and database naming best practices.
        /// Requires Configuration.Update permissions for security.
        /// </summary>
        /// <param name="request">The column name generation request containing path-to-label mappings for processing.</param>
        /// <returns>An OK result with generated column name mappings, or BadRequest for invalid input parameters.</returns>
        [HttpPost]
        [Route("GenerateColumnNames")]
        [Authorize(ReportingPermissions.Configuration.Update)]
        public IActionResult GenerateColumnNames([FromBody] GenerateColumnNamesRequest request)
        {
            try
            {
                var result = reportMappingService.GenerateColumnNames(request.PathColumns);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves preview data from a generated database view showing sample records for interface display.
        /// Fetches sample data from the first application ID found in the view with pagination and filtering support.
        /// Provides preview functionality for users to validate view structure and content before full data access.
        /// </summary>
        /// <param name="viewName">The name of the database view to query for preview data.</param>
        /// <param name="skip">The number of records to skip for pagination (defaults to 0).</param>
        /// <param name="take">The maximum number of records to return (defaults to 100).</param>
        /// <param name="filter">Optional SQL WHERE clause filter for restricting preview data.</param>
        /// <param name="orderBy">Optional SQL ORDER BY clause for sorting preview results.</param>
        /// <returns>An OK result with preview data including sample records and column information, or BadRequest for invalid view names or query parameters.</returns>
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

                var result = await reportMappingService.GetViewPreviewDataAsync(viewName, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a report mapping configuration and optionally removes the associated database view.
        /// Provides comprehensive cleanup of mapping configuration and generated database objects with configurable
        /// view deletion behavior. Ensures proper removal of both configuration data and database artifacts.
        /// Requires Configuration.Delete permissions for security.
        /// </summary>
        /// <param name="deleteViewRequest">The deletion request specifying correlation details and whether to delete the associated view.</param>
        /// <returns>An OK result with success message, NotFound if mapping doesn't exist, or BadRequest for invalid correlation parameters.</returns>
        [HttpDelete]
        [Route("Delete")]
        [Authorize(ReportingPermissions.Configuration.Delete)]
        public async Task<IActionResult> Delete([FromBody] DeleteViewRequest deleteViewRequest)
        {
            try
            {
                await reportMappingService.DeleteAsync(deleteViewRequest.CorrelationId, deleteViewRequest.CorrelationProvider, deleteViewRequest.DeleteView);
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

    /// <summary>
    /// Request model for database view generation operations containing correlation details and desired view name.
    /// Encapsulates the parameters needed to identify the mapping configuration and specify the target view name
    /// for asynchronous view generation through background job processing.
    /// </summary>
    public class GenerateViewRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity whose mapping defines the view structure.
        /// References the source entity (form version, scoresheet, etc.) that the view should be generated from.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "formversion", "scoresheet", "chefs").
        /// Determines the mapping lookup strategy and field provider for view generation operations.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the desired name for the generated database view (will be normalized to lowercase).
        /// Must be unique within the Reporting schema and conform to PostgreSQL identifier naming restrictions.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for PostgreSQL column name generation from field path-to-label mappings.
    /// Contains the input data needed to generate sanitized, unique, and PostgreSQL-compliant column names
    /// from human-readable field labels through automated processing and validation.
    /// </summary>
    public class GenerateColumnNamesRequest
    {
        /// <summary>
        /// Gets or sets the dictionary mapping field paths to their display labels for column name generation.
        /// Keys represent field paths and values are human-readable labels that will be processed into valid PostgreSQL column names.
        /// </summary>
        public Dictionary<string, string> PathColumns { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Request model for view data retrieval operations with pagination, filtering, and sorting capabilities.
    /// Provides flexible parameters for querying generated reporting views with proper data access controls
    /// and performance optimization through pagination and selective filtering.
    /// </summary>
    public class ViewDataRequest
    {
        /// <summary>
        /// Gets or sets the number of records to skip for pagination.
        /// Used in combination with Take to implement efficient pagination for large datasets.
        /// </summary>
        public int Skip { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of records to return in the query result.
        /// Provides control over result set size for performance and user interface optimization.
        /// </summary>
        public int Take { get; set; }
        
        /// <summary>
        /// Gets or sets the optional SQL WHERE clause filter for restricting query results.
        /// Should be a valid PostgreSQL WHERE clause condition without the "WHERE" keyword.
        /// </summary>
        public string? Filter { get; set; }
        
        /// <summary>
        /// Gets or sets the optional SQL ORDER BY clause for sorting query results.
        /// Should be a valid PostgreSQL ORDER BY clause without the "ORDER BY" keywords.
        /// </summary>
        public string? OrderBy { get; set; }
    }

    /// <summary>
    /// Request model for report mapping deletion operations with configurable view cleanup behavior.
    /// Specifies which mapping configuration to delete and whether to remove associated database objects
    /// for comprehensive cleanup of reporting configuration and generated artifacts.
    /// </summary>
    public class DeleteViewRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity whose mapping should be deleted.
        /// References the source entity that the mapping configuration was created for.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "formversion", "scoresheet", "chefs").
        /// Determines the mapping lookup strategy for locating the configuration to delete.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether to also delete the associated database view during mapping deletion.
        /// When true, removes both mapping configuration and generated database view; when false, preserves the view.
        /// </summary>
        public bool DeleteView { get; set; } = true;
    }
}
