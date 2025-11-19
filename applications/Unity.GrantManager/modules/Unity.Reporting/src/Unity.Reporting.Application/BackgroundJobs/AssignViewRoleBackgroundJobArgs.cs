using System;

namespace Unity.Reporting.BackgroundJobs
{
    /// <summary>
    /// Data transfer object containing parameters for the AssignViewRoleBackgroundJob.
    /// Carries correlation information and tenant context needed to assign database roles
    /// to generated reporting views for proper access control configuration.
    /// </summary>
    public class AssignViewRoleBackgroundJobArgs
    {
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "worksheet", "scoresheet", "chefs").
        /// Used to identify the source system type for the reporting view role assignment.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity (worksheet, scoresheet, or form ID).
        /// References the source entity that the reporting view was generated from.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenant context switching.
        /// Ensures role assignment operations are performed within the correct tenant scope.
        /// </summary>
        public Guid? TenantId { get; set; }
    }
}
