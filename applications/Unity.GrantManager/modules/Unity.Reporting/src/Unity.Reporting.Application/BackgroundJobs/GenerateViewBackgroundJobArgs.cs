using System;

namespace Unity.Reporting.BackgroundJobs
{
    /// <summary>
    /// Data transfer object containing parameters for the GenerateViewBackgroundJob.
    /// Carries correlation information, tenant context, and view naming details needed
    /// to generate database views from report mapping configurations and handle view renames.
    /// </summary>
    public class GenerateViewBackgroundJobArgs
    {
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "worksheet", "scoresheet", "chefs").
        /// Used to identify the source system type for the reporting view generation.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity (worksheet, scoresheet, or form ID).
        /// References the source entity that the reporting view should be generated from.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenant context switching.
        /// Ensures view generation operations are performed within the correct tenant scope.
        /// </summary>        
        public Guid? TenantId { get; set; }
        
        /// <summary>
        /// Gets or sets the original view name before any rename operation.
        /// Used to clean up old views when a view name change has occurred,
        /// preventing orphaned database objects in the Reporting schema.
        /// </summary>
        public string OriginalViewName { get; set; } = string.Empty;
    }
}
