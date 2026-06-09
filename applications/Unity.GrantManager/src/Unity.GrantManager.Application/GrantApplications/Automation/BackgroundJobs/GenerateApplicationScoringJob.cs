using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationScoringJob(
    IAIApplicationInputBuilder inputBuilder,
    IApplicationScoringService applicationScoringService,
    IApplicationRepository applicationRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    ILocalEventBus localEventBus,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType,
            args.ApplicationId,
            args.TenantId,
            args.RequestKey,
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            try
            {
                logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                var input = await inputBuilder.BuildApplicationScoringInputAsync(args.ApplicationId, args.PromptVersion);
                var scoresheetAnswers = await applicationScoringService.RegenerateAsync(input);
                var application = await applicationRepository.GetAsync(args.ApplicationId);
                application.AIScoresheetAnswers = scoresheetAnswers;
                await applicationRepository.UpdateAsync(application);
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });
                logger.LogInformation("Completed AI application scoring job for application {ApplicationId}.", args.ApplicationId);

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
