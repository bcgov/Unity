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
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Application service for managing view role assignments across multiple tenants in the reporting system.
    /// Provides functionality to retrieve tenant lists, discover reporting views, and queue background jobs
    /// for assigning database roles to generated reporting views to control access permissions.
    /// Requires IT Admin permissions for all operations.
    /// </summary>
    [Authorize(IdentityConsts.ITAdminPermissionName)]
    public class ViewRoleAssignmentAppService(
        IReportColumnsMapRepository reportColumnsMapRepository,
        IBackgroundJobManager backgroundJobManager,
        ISettingProvider settingProvider,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        ILogger<ViewRoleAssignmentAppService> logger) : ApplicationService, IViewRoleAssignmentAppService
    {
        public async Task<List<TenantDto>> GetTenantsAsync()
        {
            var tenantListResult = await tenantRepository.GetListAsync();
            return tenantListResult.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name
            }).OrderBy(t => t.Name).ToList();
        }

        public async Task<List<ReportingViewDto>> GetReportingViewsAsync(Guid tenantId)
        {
            using (currentTenant.Change(tenantId))
            {
                var reportColumnsMaps = await reportColumnsMapRepository.GetListAsync();
                var views = new List<ReportingViewDto>();

                foreach (var map in reportColumnsMaps)
                {
                    var normalizedViewName = map.ViewName.Trim().ToLowerInvariant();
                    var viewExists = await reportColumnsMapRepository.ViewExistsAsync(normalizedViewName);

                    if (viewExists)
                    {
                        views.Add(new ReportingViewDto
                        {
                            ViewName = normalizedViewName,
                            CorrelationProvider = map.CorrelationProvider,
                            CorrelationId = map.CorrelationId.ToString(),
                            RoleStatus = map.RoleStatus.ToString(),
                            HasRole = map.RoleStatus == RoleStatus.ASSIGNED
                        });
                    }
                }

                return views.OrderBy(v => v.ViewName).ToList();
            }
        }

        public async Task AssignRoleToViewsAsync(Guid tenantId, AssignRoleToViewsDto input)
        {
            using (currentTenant.Change(tenantId))
            {
                var role = await settingProvider.GetOrNullAsync(ReportingSettings.ViewRole);

                if (string.IsNullOrEmpty(role))
                {
                    throw new Volo.Abp.UserFriendlyException("No view role configured. Please configure a role first.");
                }

                var roleExists = await reportColumnsMapRepository.RoleExistsAsync(role);
                if (!roleExists)
                {
                    throw new Volo.Abp.UserFriendlyException($"Role '{role}' does not exist in the database for tenant.");
                }

                var reportColumnsMaps = await reportColumnsMapRepository.GetListAsync();
                var jobsQueued = 0;

                foreach (var viewName in input.ViewNames)
                {
                    var map = reportColumnsMaps.Find(m =>
                        m.ViewName.Trim().Equals(viewName, StringComparison.InvariantCultureIgnoreCase));

                    if (map != null)
                    {
                        // Update status to indicate we're processing
                        map.RoleStatus = RoleStatus.PENDING;
                        await reportColumnsMapRepository.UpdateAsync(map);

                        // Queue background job with the specific tenant ID
                        await backgroundJobManager.EnqueueAsync(new AssignViewRoleBackgroundJobArgs
                        {
                            CorrelationId = map.CorrelationId,
                            CorrelationProvider = map.CorrelationProvider,
                            TenantId = tenantId
                        });

                        jobsQueued++;
                        logger.LogInformation("Queued role assignment job for view: {ViewName} in tenant: {TenantId}", viewName, tenantId);
                    }
                    else
                    {
                        logger.LogWarning("No ReportColumnsMap found for view: {ViewName} in tenant: {TenantId}", viewName, tenantId);
                    }
                }

                logger.LogInformation("Queued {JobCount} role assignment jobs for tenant: {TenantId}", jobsQueued, tenantId);
            }
        }
    }
}