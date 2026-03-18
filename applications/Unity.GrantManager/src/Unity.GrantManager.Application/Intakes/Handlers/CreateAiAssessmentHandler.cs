using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;

namespace Unity.GrantManager.Intakes.Handlers;

public class CreateAiAssessmentHandler(
    AssessmentManager assessmentManager,
    IApplicationRepository applicationRepository,
    IFeatureChecker featureChecker,
    ILogger<CreateAiAssessmentHandler> logger) : ILocalEventHandler<AiScoresheetAnswersGeneratedEvent>, ITransientDependency
{
    public async Task HandleEventAsync(AiScoresheetAnswersGeneratedEvent eventData)
    {
        if (eventData == null || eventData.ApplicationId == Guid.Empty)
        {
            logger.LogWarning("Event data or application ID is null in CreateAiAssessmentHandler.");
            return;
        }

        if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
        {
            return;
        }

        try
        {
            var application = await applicationRepository.GetAsync(eventData.ApplicationId);
            await assessmentManager.CreateAiAssessmentAsync(application);
            logger.LogInformation("Created AI assessment for application {ApplicationId}.", eventData.ApplicationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating AI assessment for application {ApplicationId}.", eventData.ApplicationId);
        }
    }
}
