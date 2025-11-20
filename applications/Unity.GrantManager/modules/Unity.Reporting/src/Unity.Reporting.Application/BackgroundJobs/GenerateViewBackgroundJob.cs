using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.BackgroundJobs
{
    /// <summary>
    /// Background job for asynchronously generating database views from report mapping configurations.
    /// Handles view creation within tenant contexts, manages view name changes by cleaning up old views,
    /// updates mapping status during processing, and automatically queues role assignment upon successful completion.
    /// Provides comprehensive error handling and status tracking throughout the view generation process.
    /// </summary>
    public class GenerateViewBackgroundJob(
        ICurrentTenant currentTenant,
        IReportColumnsMapRepository reportColumnsMapRepository,
        IBackgroundJobManager backgroundJobManager,
        ILogger<GenerateViewBackgroundJob> logger) : AsyncBackgroundJob<GenerateViewBackgroundJobArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(GenerateViewBackgroundJobArgs args)
        {
            logger.LogInformation("Executing view generation function for : {CorrelationId} and {CorrelationType}", args.CorrelationId, args.CorrelationProvider);

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

                    // Check if view name has been renamed and delete the old view if needed
                    await HandleViewNameRename(args, reportColumnsMap);

                    await reportColumnsMapRepository.GenerateViewAsync(args.CorrelationId, args.CorrelationProvider);
                    reportColumnsMap.ViewStatus = Configuration.ViewStatus.SUCCESS;

                    await reportColumnsMapRepository.UpdateAsync(reportColumnsMap);

                    // Queue background job to assign view role asynchronously
                    await backgroundJobManager.EnqueueAsync(new AssignViewRoleBackgroundJobArgs
                    {                        
                        ViewName = reportColumnsMap.ViewName,
                        TenantId = currentTenant.Id
                    },
                    BackgroundJobPriority.Normal);
                }
                catch (Exception ex)
                {
                    var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(args.CorrelationId, args.CorrelationProvider);
                    if (reportColumnsMap != null)
                    {
                        reportColumnsMap.ViewStatus = Configuration.ViewStatus.FAILED;
                        await reportColumnsMapRepository.UpdateAsync(reportColumnsMap);
                    }

                    logger.LogError(ex, "Error executing view generation function for : {CorrelationId} and {CorrelationProvider}", args.CorrelationId, args.CorrelationProvider);
                }
            }
        }

        /// <summary>
        /// Handles view name renaming by deleting the old view if the name has changed.
        /// Compares the original view name from job arguments with the current view name in the mapping,
        /// and safely removes the old view if they differ, with proper error handling and logging.
        /// </summary>
        /// <param name="args">The background job arguments containing the original view name</param>
        /// <param name="reportColumnsMap">The current report columns map from the database</param>
        private async Task HandleViewNameRename(GenerateViewBackgroundJobArgs args, ReportColumnsMap reportColumnsMap)
        {
            // Check if we have an original view name and it's different from the current one
            if (!string.IsNullOrEmpty(args.OriginalViewName) &&
                !string.IsNullOrEmpty(reportColumnsMap.ViewName) &&
                !args.OriginalViewName.Equals(reportColumnsMap.ViewName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("View name has been renamed from '{OriginalViewName}' to '{NewViewName}'. Deleting old view.",
                    args.OriginalViewName, reportColumnsMap.ViewName);

                try
                {
                    // Check if the old view exists before attempting to delete it
                    var oldViewExists = await reportColumnsMapRepository.ViewExistsAsync(args.OriginalViewName);

                    if (oldViewExists)
                    {
                        await reportColumnsMapRepository.DeleteViewAsync(args.OriginalViewName);
                        logger.LogInformation("Successfully deleted old view '{OriginalViewName}'", args.OriginalViewName);
                    }
                    else
                    {
                        logger.LogInformation("Old view '{OriginalViewName}' does not exist, no deletion needed", args.OriginalViewName);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete old view '{OriginalViewName}'. The view generation will continue with the new name.", args.OriginalViewName);
                    // Don't throw here as we want the new view generation to continue even if old view deletion fails
                }
            }
        }
    }
}
