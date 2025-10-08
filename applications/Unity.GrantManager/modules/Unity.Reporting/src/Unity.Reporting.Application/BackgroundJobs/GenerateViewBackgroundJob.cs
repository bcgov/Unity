using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.BackgroundJobs
{
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
                        CorrelationId = args.CorrelationId,
                        CorrelationProvider = args.CorrelationProvider,
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
        /// Handles view name renaming by deleting the old view if the name has changed
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
