using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;

namespace Unity.GrantManager.Intakes.Handlers;

public class CreateAiAssessmentHandler(
    AssessmentManager assessmentManager,
    IFeatureChecker featureChecker,
    ILogger<CreateAiAssessmentHandler> logger) : ILocalEventHandler<AiScoresheetAnswersGeneratedEvent>, ITransientDependency
{
    public async Task HandleEventAsync(AiScoresheetAnswersGeneratedEvent eventData)
    {
        if (eventData?.Application == null)
        {
            logger.LogWarning("Event data or application is null in CreateAiAssessmentHandler.");
            return;
        }

        if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
        {
            return;
        }

        try
        {
            await assessmentManager.CreateAiAssessmentAsync(eventData.Application);
            logger.LogInformation("Created AI assessment for application {ApplicationId}.", eventData.Application.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating AI assessment for application {ApplicationId}.", eventData.Application.Id);
        }
    }
}
