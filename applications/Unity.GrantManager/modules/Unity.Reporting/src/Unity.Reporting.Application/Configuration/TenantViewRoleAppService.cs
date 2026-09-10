using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.BackgroundJobs;
using Unity.Reporting.Domain.Configuration;
using Unity.Reporting.Settings;
using Volo.Abp;
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
    private const string LicencePlateExtraPropertyKey = "LicencePlate";

    /// <summary>
    /// Retrieves the view role configuration for a specific tenant, along with the live existence
    /// state of the two roles automatically provisioned for the tenant's license plate (its
    /// read-write role and {LicencePlate}_readonly).
    /// </summary>
    public async Task<TenantViewRoleDto> GetAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetAsync(tenantId);

        var licencePlate = tenant.ExtraProperties.TryGetValue(LicencePlateExtraPropertyKey, out var lp)
            ? lp?.ToString()
            : null;
        var expectedReadOnlyRole = string.IsNullOrWhiteSpace(licencePlate) ? null : $"{licencePlate}_readonly";

        // An explicitly saved role always wins (covers legacy tenants whose role doesn't follow
        // either naming convention). Otherwise prefer the license-plate readonly role - the one
        // actually provisioned for new tenants - falling back to the legacy {tenantname}_readonly
        // pattern only when there's no license plate on record at all.
        var savedViewRole = await settingManager.GetOrNullAsync(ReportingSettings.TenantViewRole, "T", tenant.Id.ToString());

        string viewRole;
        bool isDefaultInferred;
        if (!string.IsNullOrEmpty(savedViewRole))
        {
            viewRole = savedViewRole;
            isDefaultInferred = false;
        }
        else if (expectedReadOnlyRole != null)
        {
            viewRole = expectedReadOnlyRole;
            isDefaultInferred = true;
        }
        else
        {
            viewRole = $"{tenant.Name.ToLowerInvariant()}_readonly";
            isDefaultInferred = true;
        }

        bool readOnlyRoleExists = false;
        bool mainRoleExists = false;
        if (!string.IsNullOrWhiteSpace(licencePlate))
        {
            using (currentTenant.Change(tenantId))
            {
                readOnlyRoleExists = await reportColumnsMapRepository.RoleExistsAsync(expectedReadOnlyRole!);
                mainRoleExists = await reportColumnsMapRepository.RoleExistsAsync(licencePlate);
            }
        }

        return new TenantViewRoleDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            ViewRole = viewRole,
            IsDefaultInferred = isDefaultInferred,
            LicencePlate = licencePlate,
            ExpectedReadOnlyRole = expectedReadOnlyRole,
            ReadOnlyRoleExists = readOnlyRoleExists,
            MainRoleExists = mainRoleExists
        };
    }

    /// <summary>
    /// Updates the view role configuration for a specific tenant.
    /// Sets the database role that will be granted SELECT permissions on reporting views for the tenant.
    /// </summary>
    public async Task<TenantViewRoleDto> UpdateAsync(Guid tenantId, UpdateTenantViewRoleDto input)
    {
        var tenant = await tenantRepository.GetAsync(tenantId);

        await EnsureRoleExistsAsync(tenantId, input.ViewRole);

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

        var role = await settingManager.GetOrNullAsync(ReportingSettings.TenantViewRole, "T", tenantId.ToString());
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new UserFriendlyException("No view role is configured for this tenant yet. Save a role first.");
        }

        // Fail fast here rather than relying solely on AssignViewRoleBackgroundJob's own check -
        // that check only logs a warning and silently no-ops, so without this the queue call
        // always reports success even for a role that doesn't exist in the tenant's database.
        await EnsureRoleExistsAsync(tenantId, role);

        var jobArgs = new AssignViewRoleBackgroundJobArgs
        {
            TenantId = tenantId
        };

        await backgroundJobManager.EnqueueAsync(jobArgs);
        Logger.LogInformation("Queued role assignment job for tenant: {TenantId}", tenantId);
    }

    /// <summary>
    /// Throws a <see cref="UserFriendlyException"/> if the given role does not exist as a real
    /// PostgreSQL role in the tenant's own database.
    /// </summary>
    private async Task EnsureRoleExistsAsync(Guid tenantId, string role)
    {
        using (currentTenant.Change(tenantId))
        {
            if (!await reportColumnsMapRepository.RoleExistsAsync(role))
            {
                throw new UserFriendlyException(
                    $"Role '{role}' does not exist in this tenant's database. Create the role first, or choose an existing one from View DB Info.");
            }
        }
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
