using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.ObjectMapping;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.ApplicationAnalysis;

public sealed class ApplicationAnalysisOperationExecutor(
    ApplicationAnalysisService applicationAnalysisService,
    IAIApplicationInputBuilder aiApplicationInputBuilder,
    IApplicationRepository applicationRepository,
    IUnitOfWorkManager unitOfWorkManager,
    IObjectMapper objectMapper) : AIGenerationOperationExecutor, ITransientDependency
{
    public override string OperationType => AIGenerationOperations.ApplicationAnalysis;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var application = await applicationRepository.GetAsync(args.ApplicationId);
        var promptData = objectMapper.Map<Application, AIApplicationPromptDataDto>(application);
        var analysisInput = await aiApplicationInputBuilder.BuildApplicationAnalysisInputAsync(promptData, args.PromptVersion);
        var analysisJson = await applicationAnalysisService.RegenerateAsync(analysisInput);

        await AIGenerationRequestJobHelper.SaveApplicationResultInNewUowAsync(
            unitOfWorkManager,
            applicationRepository,
            args.ApplicationId,
            app => app.AIAnalysis = analysisJson);

        return true;
    }
}
