using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Execution;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
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
    ApplicationScoringService applicationScoringService,
    IApplicationRepository applicationRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    ILocalEventBus localEventBus,
    ICooldownService aiCooldownService,
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
                args.TenantId,
                args.ApplicationId,
                args.OperationId);
            try
            {
                var application = await applicationRepository.GetAsync(args.ApplicationId);
                var scoresheetAnswers = await applicationScoringService.GenerateApplicationScoringAsync(
                    application.Id,
                    ExecutionMode.Sequential,
                    args.PromptVersion);
                application.AIScoresheetAnswers = scoresheetAnswers;
                await applicationRepository.UpdateAsync(application);
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });
                await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(aiCooldownService, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
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
