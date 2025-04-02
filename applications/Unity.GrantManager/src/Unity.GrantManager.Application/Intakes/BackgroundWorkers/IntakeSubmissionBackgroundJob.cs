using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.Logging;
using System;

namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    public class IntakeSubmissionBackgroundJob(
        IIntakeSubmissionAppService intakeSubmissionAppService,
        ICurrentTenant currentTenant,
        ILogger<IntakeSubmissionBackgroundJob> logger) : AsyncBackgroundJob<IntakeSubmissionBackgroundJobArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(IntakeSubmissionBackgroundJobArgs args)
        {
            logger.LogInformation("Executing intake submission background job for confirmation id: {ConfirmationId}", args.ConfirmationId);

            using (currentTenant.Change(args.TenantId))
            {
                try
                {
                    if (args.EventSubscriptionDto == null)
                    {
                        logger.LogWarning("EventSubscriptionDto is null");
                        return;
                    }

                    Logger.LogInformation("Processing intake in background for submissionId {Id}", args.EventSubscriptionDto.SubmissionId);

                    await intakeSubmissionAppService.CreateIntakeSubmissionAsync(args.EventSubscriptionDto);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing intake submission for confirmation id: {ConfirmationId}", args.ConfirmationId);
                }
            }
        }
    }
}