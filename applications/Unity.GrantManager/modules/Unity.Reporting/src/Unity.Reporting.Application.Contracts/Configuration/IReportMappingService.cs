using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Application service interface for managing report mappings between source fields and database columns for reporting views.
    /// Provides operations for creating, updating, retrieving, and deleting column name mappings with automatic field analysis,
    /// view generation capabilities, and comprehensive data access functionality for generated reporting views.
    /// </summary>
    public interface IReportMappingService
    {
        /// <summary>
        /// Creates a new report mapping for a specific correlation (worksheet, scoresheet, or form).
        /// Automatically generates sanitized and unique column names from field metadata and validates mapping integrity.
        /// </summary>
        /// <param name="createReportColumnsMap">The report mapping creation request containing correlation details and mapping data.</param>
        /// <returns>The created report mapping with generated column names and validation results.</returns>
        public Task<ReportColumnsMapDto> CreateAsync(UpsertReportColumnsMapDto createReportColumnsMap);
        
        /// <summary>
        /// Updates an existing report mapping with new field metadata while preserving existing column names where possible.
        /// Intelligently merges existing and new field mappings, with user-provided mappings taking precedence.
        /// </summary>
        /// <param name="updateReportColumnsMap">The report mapping update request containing correlation details and updated field mapping data.</param>
        /// <returns>The updated report mapping with merged field mappings and preserved/generated column names.</returns>
        public Task<ReportColumnsMapDto> UpdateAsync(UpsertReportColumnsMapDto updateReportColumnsMap);
        
        /// <summary>
        /// Checks whether a report mapping exists for the specified correlation (worksheet, scoresheet, or form).
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity to check.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <returns>True if a mapping exists for the specified correlation; false otherwise.</returns>
        public Task<bool> ExistsAsync(Guid correlationId, string correlationProvider);
        
        /// <summary>
        /// Retrieves an existing report mapping for a specific correlation with detected schema changes.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity (worksheet, scoresheet, or form ID).</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <returns>The report mapping with current mapping configuration and detected change information.</returns>
        public Task<ReportColumnsMapDto> GetByCorrelationAsync(Guid correlationId, string correlationProvider);
        
        /// <summary>
        /// Returns the current view name and status for a given correlation.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity.</param>
        /// <param name="correlationProvider">The provider type identifier.</param>
        /// <returns>The view status information including name and generation status.</returns>
        public Task<ReportColumnsMapViewStatusDto> GetViewStatusByCorrlationAsync(Guid correlationId, string correlationProvider);

        /// <summary>
        /// Generates sanitized and unique column names from a dictionary mapping field keys to their display labels.
        /// Column names conform to PostgreSQL naming restrictions with uniqueness enforcement.
        /// </summary>
        /// <param name="keyColumns">Dictionary mapping field keys to their human-readable display labels.</param>
        /// <returns>Dictionary mapping the same field keys to their corresponding generated PostgreSQL-compatible column names.</returns>
        public Dictionary<string, string> GenerateColumnNames(Dictionary<string, string> keyColumns);
        
        /// <summary>
        /// Retrieves comprehensive field metadata for all fields within a correlated entity (worksheet, scoresheet, or form).
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose fields should be analyzed.</param>
        /// <param name="correlationProvider">The provider type identifier that determines which fields provider to use.</param>
        /// <returns>Field metadata containing field definitions, types, paths, labels, and additional metadata for all fields.</returns>
        public Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId, string correlationProvider);

        /// <summary>
        /// Deletes a report mapping for a specific correlation and deletes the associated database view.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose mapping should be deleted.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <returns>A DeleteResult indicating what was successfully deleted.</returns>
        public Task<DeleteResult> DeleteAsync(Guid correlationId, string correlationProvider);

        /// <summary>
        /// Checks if a view name is available for use in the database by verifying it doesn't already exist.
        /// </summary>
        /// <param name="viewName">The proposed view name to check for availability in the database.</param>
        /// <returns>True if the view name is available (doesn't exist); false if the view already exists or the name is invalid.</returns>
        public Task<bool> IsViewNameAvailableAsync(string viewName);
        
        /// <summary>
        /// Checks if a view name is available for use by a specific correlation, allowing view name reuse within the same correlation.
        /// </summary>
        /// <param name="viewName">The proposed view name to check for availability.</param>
        /// <param name="correlationId">The correlation ID of the entity requesting the view name.</param>
        /// <param name="correlationProvider">The correlation provider of the entity requesting the view name.</param>
        /// <returns>True if the view name is available for this correlation; false if owned by a different correlation or name is invalid.</returns>
        public Task<bool> IsViewNameAvailableAsync(string viewName, Guid correlationId, string correlationProvider);
        
        /// <summary>
        /// Initiates asynchronous generation of a database view based on an existing report mapping configuration.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose mapping defines the view structure.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <param name="viewName">The desired name for the generated database view (will be normalized to lowercase).</param>
        /// <returns>A ViewGenerationResult indicating the view generation has been queued with status information.</returns>
        public Task<ViewGenerationResult> GenerateViewAsync(Guid correlationId, string correlationProvider, string viewName);
        
        /// <summary>
        /// Retrieves paginated and filtered data from a generated database view with support for sorting and custom filtering.
        /// </summary>
        /// <param name="viewName">The name of the database view to query for data.</param>
        /// <param name="request">The request parameters containing pagination settings, filtering criteria, and sort ordering.</param>
        /// <returns>A ViewDataResult containing the queried data rows, total record count, and column information for the requested page.</returns>
        public Task<ViewDataResult> GetViewDataAsync(string viewName, ViewDataRequest request);
        
        /// <summary>
        /// Retrieves preview data from a generated database view showing only the top record for preview purposes.
        /// </summary>
        /// <param name="viewName">The name of the database view to query for preview data.</param>
        /// <param name="request">The request parameters for filtering (pagination settings are ignored as only top 1 record is returned).</param>
        /// <returns>A ViewDataResult containing the preview data (single top record), count of 1, and column information.</returns>
        public Task<ViewDataResult> GetViewPreviewDataAsync(string viewName, ViewDataRequest request);
        
        /// <summary>
        /// Retrieves the column names and structure information from a generated database view.
        /// </summary>
        /// <param name="viewName">The name of the database view to analyze for column information.</param>
        /// <returns>An array of column names in the order they appear in the view definition.</returns>
        public Task<string[]> GetViewColumnNamesAsync(string viewName);
        
        /// <summary>
        /// Checks if a database view with the specified name exists in the Reporting schema.
        /// </summary>
        /// <param name="viewName">The name of the view to check for existence in the database.</param>
        /// <returns>True if the view exists in the Reporting schema; false if it doesn't exist or the name is invalid.</returns>
        public Task<bool> ViewExistsAsync(string viewName);
    }
    
    /// <summary>
    /// Represents a request for view data with pagination, filtering, and sorting options.
    /// Provides flexible data retrieval parameters for querying generated reporting views.
    /// </summary>
    public class ViewDataRequest
    {
        /// <summary>
        /// Gets or sets the number of records to skip for pagination.
        /// Used in combination with Take to implement pagination functionality.
        /// </summary>
        public int Skip { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the maximum number of records to return.
        /// Defaults to 100 to prevent excessive data transfer while allowing reasonable page sizes.
        /// </summary>
        public int Take { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the SQL WHERE clause filter to apply to the view query.
        /// Should be a valid PostgreSQL WHERE clause condition without the "WHERE" keyword.
        /// </summary>
        public string? Filter { get; set; }
        
        /// <summary>
        /// Gets or sets the SQL ORDER BY clause to apply for result sorting.
        /// Should be a valid PostgreSQL ORDER BY clause without the "ORDER BY" keywords.
        /// </summary>
        public string? OrderBy { get; set; }
    }
    
    /// <summary>
    /// Represents the result of a view data query including data, pagination information, and metadata.
    /// Contains both the actual data rows and contextual information about the query results.
    /// </summary>
    public class ViewDataResult
    {
        /// <summary>
        /// Gets or sets the array of data objects returned by the view query.
        /// Each object represents a row with properties corresponding to view columns.
        /// </summary>
        public object[] Data { get; set; } = [];
        
        /// <summary>
        /// Gets or sets the total number of records that match the query criteria before pagination.
        /// Used for calculating pagination controls and total page counts.
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Gets or sets the array of column names available in the view.
        /// Provides metadata about the structure of the returned data for UI generation.
        /// </summary>
        public string[] ColumnNames { get; set; } = [];
    }
    
    /// <summary>
    /// Represents the result of a delete operation indicating what was successfully removed.
    /// Provides detailed information about configuration and view deletion for accurate user feedback.
    /// </summary>
    public class DeleteResult
    {
        /// <summary>
        /// Gets or sets whether the report mapping configuration was successfully deleted.
        /// </summary>
        public bool ConfigurationDeleted { get; set; }
        
        /// <summary>
        /// Gets or sets whether a database view was successfully deleted.
        /// </summary>
        public bool ViewDeleted { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the view that was deleted, if any.
        /// </summary>
        public string? DeletedViewName { get; set; }
        
        /// <summary>
        /// Gets or sets any warning or informational messages about the deletion process.
        /// </summary>
        public string? Message { get; set; }
    }
}
