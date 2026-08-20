using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Tenants.PostCreation;

/// <summary>
/// Runs the registered <see cref="IPostTenantCreationStep"/> steps, one per job execution, in
/// ascending <see cref="IPostTenantCreationStep.Order"/> order. Each execution re-enqueues itself
/// for the next step, so the sequence is driven entirely by ABP's background job queue - every
/// step is individually durable and retryable. A step whose <see cref="IPostTenantCreationStep.ContinueOnError"/>
/// is false stops the sequence on failure; later steps are not run.
/// </summary>
public class PostTenantCreationSequenceJob(
    IEnumerable<IPostTenantCreationStep> steps,
    IBackgroundJobManager backgroundJobManager,
    ICurrentTenant currentTenant,
    ILogger<PostTenantCreationSequenceJob> logger)
    : AsyncBackgroundJob<PostTenantCreationStepArgs>, ITransientDependency
{
    private const string LogPrefix = "[PostTenantCreation]";

    public override async Task ExecuteAsync(PostTenantCreationStepArgs args)
    {
        var orderedSteps = steps.OrderBy(s => s.Order).ToList();

        if (args.StepIndex >= orderedSteps.Count)
        {
            return;
        }

        var step = orderedSteps[args.StepIndex];

        try
        {
            using (currentTenant.Change(args.TenantId))
            {
                logger.LogInformation(
                    "{Prefix} Running step {StepIndex} '{StepName}' for tenant {TenantId}",
                    LogPrefix, args.StepIndex, step.StepName, args.TenantId);

                await step.ExecuteAsync(args.TenantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "{Prefix} Step {StepIndex} '{StepName}' failed for tenant {TenantId}",
                LogPrefix, args.StepIndex, step.StepName, args.TenantId);

            if (!step.ContinueOnError)
            {
                logger.LogWarning(
                    "{Prefix} Stopping post-tenant-creation sequence after step '{StepName}' for tenant {TenantId} (ContinueOnError = false)",
                    LogPrefix, step.StepName, args.TenantId);
                return;
            }
        }

        await backgroundJobManager.EnqueueAsync(new PostTenantCreationStepArgs
        {
            TenantId = args.TenantId,
            StepIndex = args.StepIndex + 1
        });
    }
}
