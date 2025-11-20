using System;

namespace Unity.Reporting.BackgroundJobs
{
    /// <summary>
    /// Data transfer object containing parameters for the AssignViewRoleBackgroundJob.
    /// Contains tenant context needed to assign database roles to all generated reporting views 
    /// for proper access control configuration within the tenant scope.
    /// </summary>
    public class AssignViewRoleBackgroundJobArgs
    {
        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenant context switching.
        /// Ensures role assignment operations are performed within the correct tenant scope.
        /// </summary>
        public Guid? TenantId { get; set; }


        /// <summary>
        /// Gets or sets the optional view name to assign the view role to for the tenant.
        /// </summary>
        public string? ViewName { get; set; } = null;
    }
}
