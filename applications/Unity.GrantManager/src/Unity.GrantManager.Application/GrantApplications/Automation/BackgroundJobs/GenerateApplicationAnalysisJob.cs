using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.RateLimit;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationAnalysisJob(
    IApplicationAnalysisAppService applicationAnalysisAppService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateApplicationAnalysisJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            try
            {
                logger.LogInformation("Executing AI application analysis job for application {ApplicationId}.", args.ApplicationId);
                var result = await applicationAnalysisAppService.GenerateApplicationAnalysisForPipelineAsync(args.ApplicationId, args.PromptVersion);
                if (result.Completed)
                {
                    logger.LogInformation("Completed AI application analysis job for application {ApplicationId}.", args.ApplicationId);
                }

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, args.RequestKey);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey, ex.Message);
                throw;
            }
        }
    }
}
