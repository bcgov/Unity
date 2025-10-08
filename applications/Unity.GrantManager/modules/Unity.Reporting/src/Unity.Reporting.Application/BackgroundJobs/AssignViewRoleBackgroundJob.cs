using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;
using Unity.Reporting.Settings;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace Unity.Reporting.BackgroundJobs
{
    public class AssignViewRoleBackgroundJob(
        ICurrentTenant currentTenant,
        IReportColumnsMapRepository reportColumnsMapRepository,
        ISettingProvider settingProvider,
        ILogger<AssignViewRoleBackgroundJob> logger) : AsyncBackgroundJob<AssignViewRoleBackgroundJobArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(AssignViewRoleBackgroundJobArgs args)
        {
            logger.LogInformation("Assigning role to view for : {CorrelationId} and {CorrelationType}", args.CorrelationId, args.CorrelationProvider);

            using (currentTenant.Change(args.TenantId))
            {
                try
                {
                    var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(args.CorrelationId, args.CorrelationProvider);

                    if (reportColumnsMap == null)
                    {
                        logger.LogWarning("No ReportColumnsMap found for : {CorrelationId} and {CorrelationProvider}", args.CorrelationId, args.CorrelationProvider);
                        return;
                    }

                    // Check if view already exists in the Reporting schema                    
                    var normalizedViewName = reportColumnsMap.ViewName.Trim().ToLowerInvariant();
                    var viewExists = await reportColumnsMapRepository.ViewExistsAsync(normalizedViewName);

                    if (!viewExists)
                    {
                        logger.LogWarning("View {ViewName} does not exist in Reporting schema for : {CorrelationId} and {CorrelationProvider}", normalizedViewName, args.CorrelationId, args.CorrelationProvider);
                        return;
                    }

                    // Read the role from host-level settings
                    var role = await settingProvider.GetOrNullAsync(ReportingSettings.ViewRole);

                    if (string.IsNullOrEmpty(role))
                    {
                        logger.LogWarning("No role specified in settings ({SettingName}) for : {CorrelationId} and {CorrelationProvider}", ReportingSettings.ViewRole, args.CorrelationId, args.CorrelationProvider);
                        return;
                    }

                    var roleExists = await reportColumnsMapRepository.RoleExistsAsync(role);

                    if (!roleExists)
                    {
                        logger.LogWarning("Role {RoleName} does not exist in the database for : {CorrelationId} and {CorrelationProvider}", role, args.CorrelationId, args.CorrelationProvider);
                        return;
                    }

                    await reportColumnsMapRepository.AssignViewRoleAsync(normalizedViewName, role);
                    reportColumnsMap.RoleStatus = Configuration.RoleStatus.ASSIGNED;

                    await reportColumnsMapRepository.UpdateAsync(reportColumnsMap);

                    logger.LogInformation("Successfully assigned role {RoleName} to view {ViewName} for : {CorrelationId} and {CorrelationProvider}", role, normalizedViewName, args.CorrelationId, args.CorrelationProvider);
                }
                catch (Exception ex)
                {
                    var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(args.CorrelationId, args.CorrelationProvider);
                    if (reportColumnsMap != null)
                    {
                        reportColumnsMap.RoleStatus = Configuration.RoleStatus.FAILED;
                        await reportColumnsMapRepository.UpdateAsync(reportColumnsMap);
                    }

                    logger.LogError(ex, "Error executing view role assignment for : {CorrelationId} and {CorrelationProvider}", args.CorrelationId, args.CorrelationProvider);
                }
            }
        }
    }
}
