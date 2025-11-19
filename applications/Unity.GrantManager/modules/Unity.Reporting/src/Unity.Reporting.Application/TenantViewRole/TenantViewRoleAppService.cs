using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.BackgroundJobs;
using Unity.Reporting.Domain.Configuration;
using Unity.Reporting.Settings;
using Unity.Reporting.TenantViewRole;
using Unity.TenantManagement;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.SettingManagement;

namespace Unity.Reporting.Application.TenantViewRole;

/// <summary>
/// Application service for managing tenant-specific view role configurations.
/// Handles retrieval, updating, and assignment of database roles to reporting views on a per-tenant basis.
/// Requires IT Admin permissions for all operations to ensure secure configuration management.
/// </summary>
[Authorize(IdentityConsts.ITAdminPermissionName)]
public class TenantViewRoleAppService : ApplicationService, ITenantViewRoleAppService
{
    private readonly ITenantAppService _tenantAppService;
    private readonly ISettingManager _settingManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IReportColumnsMapRepository _reportColumnsMapRepository;
    private readonly ILogger<TenantViewRoleAppService> _logger;

    /// <summary>
    /// Initializes a new instance of the TenantViewRoleAppService with required dependency injection services.
    /// </summary>
    public TenantViewRoleAppService(
        ITenantAppService tenantAppService,
        ISettingManager settingManager,
        IBackgroundJobManager backgroundJobManager,
        IReportColumnsMapRepository reportColumnsMapRepository,
        ILogger<TenantViewRoleAppService> logger)
    {
        _tenantAppService = tenantAppService;
        _settingManager = settingManager;
        _backgroundJobManager = backgroundJobManager;
        _reportColumnsMapRepository = reportColumnsMapRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all tenant view role configurations.
    /// Returns a list of all tenants with their current view role assignments, defaulting to {tenantname}_readonly if not configured.
    /// </summary>
    public async Task<List<TenantViewRoleDto>> GetAllAsync()
    {
        var tenantsResult = await _tenantAppService.GetListAsync(new GetTenantsInput());
        var tenantViewRoles = new List<TenantViewRoleDto>();

        foreach (var tenant in tenantsResult.Items)
        {
            // Get tenant-specific setting first, fallback to default pattern
            var viewRole = await _settingManager.GetOrNullAsync(ReportingSettings.TenantViewRole, "T", tenant.Id.ToString());
            
            if (string.IsNullOrEmpty(viewRole))
            {
                viewRole = $"{tenant.Name.ToLowerInvariant()}_readonly";
            }

            // Check if role has been assigned to existing views
            var isAssigned = await CheckRoleAssignmentStatusAsync(tenant.Id, viewRole);

            tenantViewRoles.Add(new TenantViewRoleDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                ViewRole = viewRole,
                IsAssigned = isAssigned
            });
        }

        return tenantViewRoles;
    }

    /// <summary>
    /// Updates the view role configuration for a specific tenant.
    /// Sets the database role that will be granted SELECT permissions on reporting views for the tenant.
    /// </summary>
    public async Task<TenantViewRoleDto> UpdateAsync(Guid tenantId, UpdateTenantViewRoleDto input)
    {
        var tenant = await _tenantAppService.GetAsync(tenantId);
        
        await _settingManager.SetAsync(ReportingSettings.TenantViewRole, input.ViewRole, "T", tenantId.ToString());

        var isAssigned = await CheckRoleAssignmentStatusAsync(tenantId, input.ViewRole);

        return new TenantViewRoleDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            ViewRole = input.ViewRole,
            IsAssigned = isAssigned
        };
    }

    /// <summary>
    /// Assigns the configured role to all existing reporting views for a specific tenant.
    /// Queues background jobs to perform the role assignment operation asynchronously for all tenant views.
    /// </summary>
    public async Task AssignRoleToViewsAsync(Guid tenantId)
    {
        _logger.LogInformation("Starting role assignment for tenant: {TenantId}", tenantId);

        using (CurrentTenant.Change(tenantId))
        {
            var reportColumnMaps = await _reportColumnsMapRepository.GetListAsync();
            
            if (reportColumnMaps.Count == 0)
            {
                _logger.LogWarning("No report column maps found for tenant: {TenantId}", tenantId);
                return;
            }

            foreach (var map in reportColumnMaps)
            {
                var jobArgs = new AssignViewRoleBackgroundJobArgs
                {
                    TenantId = tenantId,
                    CorrelationId = map.CorrelationId,
                    CorrelationProvider = map.CorrelationProvider
                };

                await _backgroundJobManager.EnqueueAsync(jobArgs);
                _logger.LogDebug("Queued role assignment job for correlation: {CorrelationId}, Provider: {CorrelationProvider}", 
                    map.CorrelationId, map.CorrelationProvider);
            }
        }

        _logger.LogInformation("Completed queuing role assignment jobs for tenant: {TenantId}", tenantId);
    }

    /// <summary>
    /// Checks if a role has been assigned to existing views for a tenant.
    /// This is a simplified check - in a real implementation you might check the actual database state.
    /// </summary>
    private async Task<bool> CheckRoleAssignmentStatusAsync(Guid tenantId, string viewRole)
    {
        try
        {
            using (CurrentTenant.Change(tenantId))
            {
                var reportColumnMaps = await _reportColumnsMapRepository.GetListAsync();
                return reportColumnMaps.Any(map => map.RoleStatus == Configuration.RoleStatus.ASSIGNED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking role assignment status for tenant: {TenantId}", tenantId);
            return false;
        }
    }
}