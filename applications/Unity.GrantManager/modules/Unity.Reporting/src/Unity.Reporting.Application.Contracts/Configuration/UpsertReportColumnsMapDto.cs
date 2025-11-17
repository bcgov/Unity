using System;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object for creating or updating report column mapping configurations.
    /// Contains correlation identification and optional field mapping overrides for customizing
    /// how source fields map to database columns in generated reporting views.
    /// </summary>
    public class UpsertReportColumnsMapDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity (worksheet, scoresheet, or form ID).
        /// Links this mapping request to its source entity in the respective system for field metadata retrieval.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "worksheet", "scoresheet", "chefs").
        /// Identifies the source system type that determines which field provider will be used for metadata extraction.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;
       
        /// <summary>
        /// Gets or sets the optional column mapping configuration containing user-specified field-to-column mappings.
        /// When provided, these mappings override auto-generated column names for specific fields.
        /// Empty mappings will result in fully auto-generated column names based on field labels.
        /// </summary>
        public UpsertColumnMappingDto Mapping { get; set; } = new();
    }
}
