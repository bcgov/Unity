using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.BackgroundJobs;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Application service for tenant-specific reporting view roles: shows which role a tenant's
/// reporting views are granted to, and re-grants it on all of them. The role comes from
/// <see cref="TenantViewRoleManager"/> (the saved setting, else {LicencePlate}_readonly).
/// Requires the IT Administrator or IT Operations role for all operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TenantViewRoleAppService with required dependency injection services.
/// </remarks>
[Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]
public class TenantViewRoleAppService(
    ITenantRepository tenantRepository,
    TenantViewRoleManager tenantViewRoleManager,
    IBackgroundJobManager backgroundJobManager,
    IReportColumnsMapRepository reportColumnsMapRepository,
    ICurrentTenant currentTenant) : ApplicationService, ITenantViewRoleAppService
{
    /// <summary>
    /// Retrieves the role a tenant's reporting views are granted to, along with the live existence
    /// state of that role and of the two roles automatically provisioned for the tenant's license
    /// plate (its read-write role and {LicencePlate}_readonly).
    /// </summary>
    public async Task<TenantViewRoleDto> GetAsync(Guid tenantId)
    {
        return await BuildViewRoleAsync(await tenantRepository.GetAsync(tenantId));
    }

    private async Task<TenantViewRoleDto> BuildViewRoleAsync(Tenant tenant)
    {
        var licencePlate = TenantViewRoleManager.GetLicencePlate(tenant);
        var expectedReadOnlyRole = licencePlate == null ? null : $"{licencePlate}_readonly";
        var viewRole = await tenantViewRoleManager.ResolveAsync(tenant.Id);

        bool viewRoleExists = false;
        bool readOnlyRoleExists = false;
        bool mainRoleExists = false;
        using (currentTenant.Change(tenant.Id))
        {
            if (viewRole != null)
            {
                viewRoleExists = await reportColumnsMapRepository.RoleExistsAsync(viewRole);
            }
            if (licencePlate != null)
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
            ViewRoleExists = viewRoleExists,
            LicencePlate = licencePlate,
            ExpectedReadOnlyRole = expectedReadOnlyRole,
            ReadOnlyRoleExists = readOnlyRoleExists,
            MainRoleExists = mainRoleExists
        };
    }

    /// <summary>
    /// Grants the tenant's view role on all existing reporting views.
    /// Queues a background job to perform the role assignment operation asynchronously for all tenant views.
    /// </summary>
    public async Task AssignRoleToViewsAsync(Guid tenantId)
    {
        Logger.LogInformation("Starting role assignment for tenant: {TenantId}", tenantId);

        var role = await tenantViewRoleManager.ResolveAsync(tenantId)
            ?? throw new UserFriendlyException(
                "This tenant has no reporting view role: it has no licence plate and no saved role.");

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
                    $"Role '{role}' does not exist in this tenant's database. Check the roles in View DB Info.");
            }
        }
    }

    /// <summary>
    /// Retrieves the reporting health of a tenant's database: the view role and whether it exists,
    /// and every reporting view reconciled against the mappings that generate them - flagging views
    /// whose generation failed or is stuck, mapped views missing from the schema, views no mapping
    /// uses, and views the view role cannot read.
    /// </summary>
    public async Task<TenantDatabaseInfoDto> GetTenantDatabaseInfoAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetAsync(tenantId);

        var databaseInfo = new TenantDatabaseInfoDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name
        };

        try
        {
            databaseInfo.Role = await BuildViewRoleAsync(tenant);
            var viewRole = databaseInfo.Role.ViewRoleExists ? databaseInfo.Role.ViewRole : null;

            using (currentTenant.Change(tenantId))
            {
                if (viewRole != null)
                {
                    var access = await reportColumnsMapRepository.GetRoleAccessAsync(viewRole);
                    databaseInfo.ViewRoleCanLogin = access?.CanLogin ?? false;
                    databaseInfo.ViewRoleHasSchemaUsage = access?.HasSchemaUsage ?? false;
                }

                // has_table_privilege errors on a role that doesn't exist, so only check grants
                // against one that does.
                var grants = await reportColumnsMapRepository.GetReportingViewGrantsAsync(viewRole);
                var maps = await reportColumnsMapRepository.GetListAsync();

                databaseInfo.Views = BuildViewStatuses(grants, maps, viewRole != null);
            }

            Logger.LogInformation("Retrieved database info for tenant {TenantId}: {ViewCount} views, {IssueCount} with issues",
                tenantId, databaseInfo.Views.Count, databaseInfo.Views.Count(v => v.Issue != ReportingViewIssue.None));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving database information for tenant {TenantId}", tenantId);
            // Return what we have rather than throwing, so the UI can still display
            databaseInfo.Error = "Could not read the tenant's reporting database. Check the logs for details.";
        }

        return databaseInfo;
    }

    private static List<ReportingViewStatusDto> BuildViewStatuses(
        List<ReportingViewGrant> grants, List<ReportColumnsMap> maps, bool checkGrants)
    {
        var grantsByName = grants.ToDictionary(g => g.ViewName, StringComparer.OrdinalIgnoreCase);
        var mapsByName = maps
            .Where(m => !string.IsNullOrWhiteSpace(m.ViewName))
            .GroupBy(m => m.ViewName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return grantsByName.Keys
            .Union(mapsByName.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                grantsByName.TryGetValue(name, out var grant);
                mapsByName.TryGetValue(name, out var map);
                bool? granted = grant != null && checkGrants ? grant.HasSelect : null;

                return new ReportingViewStatusDto
                {
                    ViewName = grant?.ViewName ?? name,
                    CorrelationProvider = map?.CorrelationProvider,
                    ViewStatus = map?.ViewStatus,
                    InDatabase = grant != null,
                    Granted = granted,
                    Issue = (map?.ViewStatus, grant != null, granted) switch
                    {
                        (ViewStatus.FAILED, _, _) => ReportingViewIssue.GenerationFailed,
                        (ViewStatus.GENERATING, _, _) => ReportingViewIssue.Generating,
                        (ViewStatus.SUCCESS, false, _) => ReportingViewIssue.Missing,
                        (null, true, _) => ReportingViewIssue.Unmapped,
                        (_, _, false) => ReportingViewIssue.NotGranted,
                        _ => ReportingViewIssue.None
                    }
                };
            })
            .OrderBy(v => v.Issue == ReportingViewIssue.None)
            .ThenBy(v => v.ViewName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
