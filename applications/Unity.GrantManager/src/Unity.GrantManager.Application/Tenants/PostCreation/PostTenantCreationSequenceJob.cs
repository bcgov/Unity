using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;

namespace Unity.GrantManager.Tenants.PostCreation;

/// <summary>
/// Runs the registered <see cref="IPostTenantCreationStep"/> steps, one per job execution, in
/// ascending <see cref="IPostTenantCreationStep.Order"/> order. Each execution re-enqueues itself
/// for the next step, so the sequence is driven entirely by ABP's background job queue.
///
/// A step's exception is always caught and logged here rather than rethrown, so ABP's own
/// background-job retry mechanism (which only engages when <c>ExecuteAsync</c> throws) never
/// applies to an individual step - failures are best-effort, not retried automatically. A step
/// whose <see cref="IPostTenantCreationStep.ContinueOnError"/> is true is logged and the sequence
/// moves on to the next step regardless; one whose ContinueOnError is false stops the sequence
/// entirely on failure (later steps do not run). Either way, the failed step itself does not get
/// another automatic attempt - recovering it currently requires manually re-enqueuing a
/// <see cref="PostTenantCreationStepArgs"/> for that step index.
///
/// Every outcome (skip aside - see below) is also persisted onto the tenant's "Sections" status
/// (<see cref="TenantPostCreationSectionsExtensions"/>), so an admin can see step state from the
/// Tenants screen without reading logs. A step skipped via <c>CanExecuteAsync</c> returning false
/// is left as "Waiting" rather than marked as a result, since it may still run on a later manual
/// re-enqueue (e.g. once required configuration is added).
/// </summary>
public class PostTenantCreationSequenceJob(
    IEnumerable<IPostTenantCreationStep> steps,
    IBackgroundJobManager backgroundJobManager,
    ITenantRepository tenantRepository,
    ICurrentTenant currentTenant,
    IClock clock,
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
                if (!await step.CanExecuteAsync(args.TenantId))
                {
                    logger.LogInformation(
                        "{Prefix} Skipping step {StepIndex} '{StepName}' for tenant {TenantId} - CanExecuteAsync returned false",
                        LogPrefix, args.StepIndex, step.StepName, args.TenantId);
                }
                else
                {
                    logger.LogInformation(
                        "{Prefix} Running step {StepIndex} '{StepName}' for tenant {TenantId}",
                        LogPrefix, args.StepIndex, step.StepName, args.TenantId);

                    await step.ExecuteAsync(args.TenantId);
                    await UpdateStepStatusAsync(args.TenantId, step, PostTenantCreationStepStatus.Success, null);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "{Prefix} Step {StepIndex} '{StepName}' failed for tenant {TenantId}",
                LogPrefix, args.StepIndex, step.StepName, args.TenantId);

            await UpdateStepStatusAsync(args.TenantId, step, PostTenantCreationStepStatus.Error, ex.Message);

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

    private async Task UpdateStepStatusAsync(
        Guid tenantId, IPostTenantCreationStep step, PostTenantCreationStepStatus status, string? message)
    {
        // Tenant is host-side data. The success path calls this from inside the
        // currentTenant.Change(args.TenantId) block above, so without forcing back to the host
        // context here, ITenantRepository could resolve against the wrong DB/connection - the
        // same reason ProgramDetailsAppService wraps its own Tenant repository calls in
        // CurrentTenant.Change(null).
        using (currentTenant.Change(null))
        {
            var tenant = await tenantRepository.GetAsync(tenantId);
            tenant.SetPostTenantCreationStepStatus(step.Key, step.StepName, status, message, clock.Now);
            await tenantRepository.UpdateAsync(tenant);
        }
    }
}
