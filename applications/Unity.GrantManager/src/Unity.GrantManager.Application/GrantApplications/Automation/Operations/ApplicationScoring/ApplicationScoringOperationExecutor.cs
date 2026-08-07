using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.ObjectMapping;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.ApplicationScoring;

public sealed class ApplicationScoringOperationExecutor(
    ApplicationScoringService applicationScoringService,
    IAIApplicationInputBuilder aiApplicationInputBuilder,
    IApplicationRepository applicationRepository,
    IUnitOfWorkManager unitOfWorkManager,
    ILocalEventBus localEventBus,
    IObjectMapper objectMapper) : AIGenerationOperationExecutor, ITransientDependency
{
    public override string OperationType => AIGenerationOperations.ApplicationScoring;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var application = await applicationRepository.GetAsync(args.ApplicationId);
        var promptData = objectMapper.Map<Application, AIApplicationPromptDataDto>(application);
        var scoringInput = await aiApplicationInputBuilder.BuildApplicationScoringInputAsync(promptData, args.PromptVersion);
        var scoresheetAnswers = await applicationScoringService.RegenerateAsync(scoringInput);

        await AIGenerationRequestJobHelper.SaveApplicationResultInNewUowAsync(
            unitOfWorkManager,
            applicationRepository,
            args.ApplicationId,
            app => app.AIScoresheetAnswers = scoresheetAnswers);

        await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
        {
            ApplicationId = args.ApplicationId
        });

        return true;
    }
}
