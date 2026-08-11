using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.AI.RateLimit;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class AIGenerationBackgroundJob(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<AIGenerationBackgroundJob> logger,
    IAIGenerationOperationExecutorRegistry operationExecutorRegistry) : AsyncBackgroundJob<AIGenerationBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            args.OperationType,
            args.ApplicationId,
            args.TenantId,
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(
                unitOfWorkManager,
                generationRequestRepository,
                args.TenantId,
                args.ApplicationId,
                args.OperationId);

            try
            {
                var executor = operationExecutorRegistry.Resolve(args.OperationType);
                if (await executor.ExecuteAsync(args))
                {
                    await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(
                        aiRateLimiter,
                        logger,
                        args.RequestedByUserId,
                        args.ApplicationId,
                        args.OperationType);
                }

                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId,
                    ex.Message);
                throw;
            }
        }
    }
}
