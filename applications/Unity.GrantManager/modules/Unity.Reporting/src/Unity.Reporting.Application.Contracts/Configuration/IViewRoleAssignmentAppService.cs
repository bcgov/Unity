using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Application service interface for managing database role assignments to generated reporting views across multiple tenants.
    /// Provides functionality to retrieve tenant information, discover reporting views, and queue background jobs
    /// for assigning database roles to views to control access permissions in multi-tenant environments.
    /// </summary>
    public interface IViewRoleAssignmentAppService : IApplicationService
    {
        /// <summary>
        /// Retrieves a list of all available tenants in the system for role assignment operations.
        /// </summary>
        /// <returns>A list of tenant DTOs containing basic tenant information (ID and name) ordered by tenant name.</returns>
        Task<List<TenantDto>> GetTenantsAsync();
        
        /// <summary>
        /// Retrieves a list of reporting views available within a specific tenant context.
        /// Discovers views from existing report mappings and checks their database existence status.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant to query for reporting views.</param>
        /// <returns>A list of reporting view DTOs with view information, correlation details, and current role assignment status.</returns>
        Task<List<ReportingViewDto>> GetReportingViewsAsync(Guid tenantId);
        
        /// <summary>
        /// Queues background jobs to assign configured database roles to selected reporting views within a tenant context.
        /// Validates role existence and updates mapping status during the assignment process.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant where role assignments should occur.</param>
        /// <param name="input">The request containing the list of view names to assign roles to.</param>
        Task AssignRoleToViewsAsync(Guid tenantId, AssignRoleToViewsDto input);
    }

    /// <summary>
    /// Data transfer object representing basic tenant information for role assignment operations.
    /// Contains essential tenant details needed for tenant selection and identification in multi-tenant scenarios.
    /// </summary>
    public class TenantDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the tenant.
        /// Used for tenant context switching and role assignment operations.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the tenant.
        /// Used for user-friendly identification in tenant selection interfaces.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object representing a reporting view with its correlation information and role assignment status.
    /// Provides comprehensive information about generated views including their source correlation and access control status.
    /// </summary>
    public class ReportingViewDto
    {
        /// <summary>
        /// Gets or sets the name of the database view in the Reporting schema.
        /// Normalized to lowercase for consistent database operations and identification.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "worksheet", "scoresheet", "chefs").
        /// Identifies the source system type that this view was generated from.
        /// </summary>
        public string? CorrelationProvider { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation identifier as a string representation.
        /// References the source entity ID that the view was generated from.
        /// </summary>
        public string? CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the current role assignment status as a string representation.
        /// Indicates whether roles are assigned, pending, failed, or not assigned to the view.
        /// </summary>
        public string? RoleStatus { get; set; }
        
        /// <summary>
        /// Gets or sets a boolean flag indicating whether the view currently has assigned database roles.
        /// True if roles are successfully assigned; false if roles are not assigned, pending, or failed.
        /// </summary>
        public bool HasRole { get; set; }
    }

    /// <summary>
    /// Data transfer object for requesting role assignment to multiple reporting views.
    /// Contains the list of view names that should have database roles assigned for access control.
    /// </summary>
    public class AssignRoleToViewsDto
    {
        /// <summary>
        /// Gets or sets the list of view names that should have database roles assigned.
        /// Each view name should correspond to an existing view in the Reporting schema within the target tenant.
        /// </summary>
        public List<string> ViewNames { get; set; } = new();
    }
}