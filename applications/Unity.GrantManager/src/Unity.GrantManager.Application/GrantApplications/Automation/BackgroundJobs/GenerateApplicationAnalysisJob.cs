using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationAnalysisJob(
    ApplicationAnalysisService applicationAnalysisService,
    IApplicationRepository applicationRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    ICooldownService aiCooldownService,
    ILogger<GenerateApplicationAnalysisJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType,
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
                var analysisJson = await applicationAnalysisService.GenerateApplicationAnalysisAsync(application.Id, args.PromptVersion);
                application.AIAnalysis = analysisJson;
                await applicationRepository.UpdateAsync(application);
                await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(aiCooldownService, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);
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
