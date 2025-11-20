using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Reporting.Web.Controllers;

/// <summary>
/// HTTP API controller for managing tenant-specific view role configurations.
/// Provides RESTful endpoints for retrieving, updating, and assigning database roles to reporting views per tenant.
/// All operations require IT Admin permissions for secure configuration management.
/// </summary>
[Area("reporting")]
[Route("api/reporting/tenant-view-roles")]
public class TenantViewRoleController : AbpControllerBase
{
    private readonly ITenantViewRoleAppService _tenantViewRoleAppService;

    /// <summary>
    /// Initializes a new instance of the TenantViewRoleController.
    /// </summary>
    /// <param name="tenantViewRoleAppService">The application service for tenant view role management.</param>
    public TenantViewRoleController(ITenantViewRoleAppService tenantViewRoleAppService)
    {
        _tenantViewRoleAppService = tenantViewRoleAppService;
    }

    /// <summary>
    /// Retrieves all tenant view role configurations.
    /// </summary>
    /// <returns>A list of all tenant view role configurations.</returns>
    [HttpGet]
    public Task<List<TenantViewRoleDto>> GetAllAsync()
    {
        return _tenantViewRoleAppService.GetAllAsync();
    }

    /// <summary>
    /// Updates the view role configuration for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="input">The updated view role configuration.</param>
    /// <returns>The updated tenant view role configuration.</returns>
    [HttpPut("{tenantId}")]
    public Task<TenantViewRoleDto> UpdateAsync(Guid tenantId, UpdateTenantViewRoleDto input)
    {
        return _tenantViewRoleAppService.UpdateAsync(tenantId, input);
    }

    /// <summary>
    /// Assigns the configured role to all existing reporting views for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("{tenantId}/assign-role-to-views")]
    public Task AssignRoleToViewsAsync(Guid tenantId)
    {
        return _tenantViewRoleAppService.AssignRoleToViewsAsync(tenantId);
    }
}