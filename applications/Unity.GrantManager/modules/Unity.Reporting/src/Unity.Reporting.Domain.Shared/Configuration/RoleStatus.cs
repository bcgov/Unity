using System.Text.Json.Serialization;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Enumeration representing the current status of database role assignment operations for generated reporting views.
    /// Tracks the lifecycle of role assignment from initial state through completion or failure, enabling proper
    /// access control management and status monitoring for view permissions in multi-tenant environments.
    /// Configured with JSON string enum converter for proper API serialization and human-readable values.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleStatus
    {
        /// <summary>
        /// Indicates that no database role has been assigned to the generated view.
        /// This is the default status for newly created view mappings before role assignment operations begin.
        /// Views in this status may not be accessible to end users until roles are properly assigned.
        /// </summary>
        NOTASSIGNED = 0,
        
        /// <summary>
        /// Indicates that database roles have been successfully assigned to the generated view.
        /// The view is properly configured with access control permissions and is available for authorized users.
        /// This represents the successful completion of both view generation and role assignment processes.
        /// </summary>
        ASSIGNED = 1,
        
        /// <summary>
        /// Indicates that the database role assignment process failed due to an error.
        /// This could be due to missing roles in the database, permission issues, or other technical problems.
        /// Views in this status may exist but lack proper access control configuration.
        /// </summary>
        FAILED = 2,
        
        /// <summary>
        /// Indicates that the database role assignment process has been initiated but is not yet complete.
        /// A background job is processing the role assignment operation and the final status is pending.
        /// This is a transitional status between NOTASSIGNED and either ASSIGNED or FAILED.
        /// </summary>
        PENDING = 3
    }
}
