using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Reporting.TenantViewRole;

/// <summary>
/// Application service interface for managing tenant-specific view role configurations.
/// Provides operations for retrieving, updating, and assigning database roles to reporting views per tenant.
/// </summary>
public interface ITenantViewRoleAppService : IApplicationService
{
    /// <summary>
    /// Retrieves all tenant view role configurations.
    /// Returns a list of all tenants with their current view role assignments.
    /// </summary>
    /// <returns>A list of tenant view role configurations.</returns>
    Task<List<TenantViewRoleDto>> GetAllAsync();

    /// <summary>
    /// Updates the view role configuration for a specific tenant.
    /// Sets the database role that will be granted SELECT permissions on reporting views for the tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="input">The updated view role configuration.</param>
    /// <returns>The updated tenant view role configuration.</returns>
    Task<TenantViewRoleDto> UpdateAsync(Guid tenantId, UpdateTenantViewRoleDto input);

    /// <summary>
    /// Assigns the configured role to all existing reporting views for a specific tenant.
    /// Queues a background job to perform the role assignment operation asynchronously.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AssignRoleToViewsAsync(Guid tenantId);
}