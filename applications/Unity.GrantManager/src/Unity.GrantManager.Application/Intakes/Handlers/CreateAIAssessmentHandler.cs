using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes.Handlers;

public class CreateAIAssessmentHandler(
    AssessmentManager assessmentManager,
    IApplicationRepository applicationRepository,
    IFeatureChecker featureChecker,
    ISettingProvider settingProvider,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<CreateAIAssessmentHandler> logger) : ILocalEventHandler<AIApplicationScoringGeneratedEvent>, ITransientDependency
{
    public async Task HandleEventAsync(AIApplicationScoringGeneratedEvent eventData)
    {
        if (eventData == null || eventData.ApplicationId == Guid.Empty)
        {
            logger.LogWarning("Event data or application ID is null in CreateAIAssessmentHandler.");
            return;
        }

        if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
        {
            return;
        }

        if (!await settingProvider.GetAsync<bool>(AISettings.ScoringAssistantEnabled, defaultValue: false))
        {
            return;
        }

        try
        {
            using var uow = unitOfWorkManager.Begin(requiresNew: true);
            var application = await applicationRepository.GetAsync(eventData.ApplicationId);
            await assessmentManager.CreateAiAssessmentAsync(application);
            await uow.CompleteAsync();
            logger.LogInformation("Created AI assessment for application {ApplicationId}.", eventData.ApplicationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating AI assessment for application {ApplicationId}.", eventData.ApplicationId);
        }
    }
}
