using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.BackgroundJobs;
using Unity.Reporting.Domain.Configuration;
using Unity.Reporting.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Application service for managing tenant-specific view role configurations.
/// Handles retrieval, updating, and assignment of database roles to reporting views on a per-tenant basis.
/// Requires IT Admin permissions for all operations to ensure secure configuration management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TenantViewRoleAppService with required dependency injection services.
/// </remarks>
[Authorize(IdentityConsts.ITAdminPermissionName)]
public class TenantViewRoleAppService(
    ITenantRepository tenantRepository,
    ISettingManager settingManager,
    IBackgroundJobManager backgroundJobManager,
    IReportColumnsMapRepository reportColumnsMapRepository,
    ICurrentTenant currentTenant) : ApplicationService, ITenantViewRoleAppService
{
    /// <summary>
    /// Retrieves all tenant view role configurations.
    /// Returns a list of all tenants with their current view role assignments, defaulting to {tenantname}_readonly if not configured.
    /// </summary>
    public async Task<List<TenantViewRoleDto>> GetAllAsync()
    {
        var tenants = await tenantRepository.GetListAsync();
        var tenantViewRoles = new List<TenantViewRoleDto>();

        foreach (var tenant in tenants)
        {
            // Get tenant-specific setting first, fallback to default pattern
            var viewRole = await settingManager.GetOrNullAsync(ReportingSettings.TenantViewRole, "T", tenant.Id.ToString());

            bool isDefaultInferred = false;
            if (string.IsNullOrEmpty(viewRole))
            {
                viewRole = $"{tenant.Name.ToLowerInvariant()}_readonly";
                isDefaultInferred = true;
            }

            tenantViewRoles.Add(new TenantViewRoleDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                ViewRole = viewRole,
                IsDefaultInferred = isDefaultInferred
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
        var tenant = await tenantRepository.GetAsync(tenantId);

        await settingManager.SetAsync(ReportingSettings.TenantViewRole, input.ViewRole, "T", tenantId.ToString());

        return new TenantViewRoleDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            ViewRole = input.ViewRole,
            IsDefaultInferred = false // After saving, it's no longer inferred
        };
    }

    /// <summary>
    /// Assigns the configured role to all existing reporting views for a specific tenant.
    /// Queues a background job to perform the role assignment operation asynchronously for all tenant views.
    /// </summary>
    public async Task AssignRoleToViewsAsync(Guid tenantId)
    {
        Logger.LogInformation("Starting role assignment for tenant: {TenantId}", tenantId);

        var jobArgs = new AssignViewRoleBackgroundJobArgs
        {
            TenantId = tenantId
        };

        await backgroundJobManager.EnqueueAsync(jobArgs);
        Logger.LogInformation("Queued role assignment job for tenant: {TenantId}", tenantId);
    }

    /// <summary>
    /// Retrieves database information for a specific tenant, including available roles and reporting views.
    /// This method queries the tenant's database to return a comprehensive list of database roles
    /// and all reporting views in the Reporting schema for the specified tenant.
    /// </summary>
    public async Task<TenantDatabaseInfoDto> GetTenantDatabaseInfoAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetAsync(tenantId);

        var databaseInfo = new TenantDatabaseInfoDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            DatabaseRoles = [],
            ReportingViews = []
        };

        try
        {
            // Set the current tenant context for database operations
            using (currentTenant.Change(tenantId))
            {
                // Get database roles using repository
                var roles = await reportColumnsMapRepository.GetDatabaseRolesAsync();
                
                // Get role memberships using repository
                var memberships = await reportColumnsMapRepository.GetRoleMembershipsAsync();
                
                // Combine roles and memberships
                var allRoles = new List<string>();
                allRoles.AddRange(roles);
                
                if (memberships.Count > 0)
                {
                    allRoles.Add("--- Role Memberships ---");
                    allRoles.AddRange(memberships);
                }
                
                databaseInfo.DatabaseRoles = allRoles;

                // Get reporting views using repository
                databaseInfo.ReportingViews = await reportColumnsMapRepository.GetReportingViewsAsync();
            }

            Logger.LogInformation("Retrieved database info for tenant {TenantId}: {RoleCount} roles, {ViewCount} views", 
                tenantId, databaseInfo.DatabaseRoles.Count, databaseInfo.ReportingViews.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving database information for tenant {TenantId}", tenantId);
            // Return empty lists rather than throwing, so the UI can still display
        }

        return databaseInfo;
    }
}
