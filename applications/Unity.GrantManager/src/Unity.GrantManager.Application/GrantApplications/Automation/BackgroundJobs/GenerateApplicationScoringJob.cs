using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
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
using Volo.Abp.ObjectMapping;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationScoringJob(
    IAIApplicationInputBuilder inputBuilder,
    IApplicationScoringService applicationScoringService,
    IApplicationRepository applicationRepository,
    IObjectMapper objectMapper,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
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
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(
                unitOfWorkManager,
                generationRequestRepository,
                operationRepository,
                args.TenantId,
                args.ApplicationId,
                AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
            try
            {
                var application = await applicationRepository.GetAsync(args.ApplicationId);
                var applicationInput = objectMapper.Map<Application, AIApplicationPromptDataDto>(application);
                var input = await inputBuilder.BuildApplicationScoringInputAsync(applicationInput, args.PromptVersion);
                var scoresheetAnswers = await applicationScoringService.RegenerateAsync(input);
                application.AIScoresheetAnswers = scoresheetAnswers;
                await applicationRepository.UpdateAsync(application);
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });
                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.ApplicationScoringOperationType,
                    ex.Message);
                throw;
            }
        }
    }
}
