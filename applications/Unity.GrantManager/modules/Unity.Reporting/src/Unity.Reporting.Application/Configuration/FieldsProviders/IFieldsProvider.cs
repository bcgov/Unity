using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Interface defining contract for field metadata providers that extract and analyze field information
    /// from different correlation providers (worksheets, scoresheets, forms) in the Unity system.
    /// Enables pluggable field discovery and change detection for dynamic report mapping configuration.
    /// </summary>
    public interface IFieldsProvider
    {
        /// <summary>
        /// Retrieves comprehensive field metadata for all fields within a correlated entity.
        /// Extracts field definitions, types, paths, labels, and hierarchical structure information
        /// from the source system (worksheet, scoresheet, form) for use in report mapping configuration.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity to analyze.</param>
        /// <returns>A field path metadata map containing field definitions and additional context information.</returns>
        Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId);
        
        /// <summary>
        /// Detects and summarizes changes between current field structure and previously stored mapping configuration.
        /// Compares the current state of fields/structure against the stored mapping to identify additions,
        /// removals, or modifications that may affect the reporting configuration.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity to check for changes.</param>
        /// <param name="reportColumnsMap">The existing report columns mapping configuration to compare against.</param>
        /// <returns>A change description string if differences are detected, null if no changes are found.</returns>
        Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap);

        /// <summary>
        /// Gets the correlation provider identifier that this fields provider handles.
        /// Used by the system to route field metadata requests to the appropriate provider
        /// based on the correlation provider type (e.g., "worksheet", "scoresheet", "chefs").
        /// </summary>
        string CorrelationProvider { get; }
    }
}
