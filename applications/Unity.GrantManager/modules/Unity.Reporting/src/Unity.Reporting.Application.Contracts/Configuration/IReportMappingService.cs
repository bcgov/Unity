using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Reporting.Configuration
{
    public interface IReportMappingService
    {
        public Task<ReportColumnsMapDto> CreateAsync(UpsertReportColumnsMapDto createReportColumnsMap);
        public Task<ReportColumnsMapDto> UpdateAsync(UpsertReportColumnsMapDto updateReportColumnsMap);
        
        public Task<bool> ExistsAsync(Guid correlationId, string correlationProvider);
        public Task<ReportColumnsMapDto> GetByCorrelationAsync(Guid correlationId, string correlationProvider);
        public Task<ReportColumnsMapViewStatusDto> GetViewStatusByCorrlationAsync(Guid correlationId, string correlationProvider);

        public Dictionary<string, string> GenerateColumnNames(Dictionary<string, string> keyColumns);
        public Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId, string correlationProvider);

        // Delete mapping and optionally delete associated view
        public Task DeleteAsync(Guid correlationId, string correlationProvider, bool deleteView = true);

        // View generation endpoints
        public Task<bool> IsViewNameAvailableAsync(string viewName);
        public Task<bool> IsViewNameAvailableAsync(string viewName, Guid correlationId, string correlationProvider);
        public Task<ViewGenerationResult> GenerateViewAsync(Guid correlationId, string correlationProvider, string viewName);
        
        // View data retrieval endpoints
        public Task<ViewDataResult> GetViewDataAsync(string viewName, ViewDataRequest request);
        public Task<ViewDataResult> GetViewPreviewDataAsync(string viewName, ViewDataRequest request);
        public Task<string[]> GetViewColumnNamesAsync(string viewName);
        public Task<bool> ViewExistsAsync(string viewName);
    }
    
    /// <summary>
    /// Represents a request for view data with pagination and filtering options
    /// </summary>
    public class ViewDataRequest
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 100;
        public string? Filter { get; set; }
        public string? OrderBy { get; set; }
    }
    
    /// <summary>
    /// Represents the result of a view data query
    /// </summary>
    public class ViewDataResult
    {
        public object[] Data { get; set; } = [];
        public int TotalCount { get; set; }
        public string[] ColumnNames { get; set; } = [];
    }
}
