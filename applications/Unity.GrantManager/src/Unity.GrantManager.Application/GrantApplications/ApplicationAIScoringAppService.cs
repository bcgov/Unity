using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.ScoringAssistant.ScoringAssistantDefault)]
public class ApplicationAIScoringAppService(
    IApplicationScoresheetAnalysisService applicationScoresheetAnalysisService,
    IApplicationRepository applicationRepository,
    ILocalEventBus localEventBus,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationAIScoringAppService
{
    public async Task<string> GenerateAIScoresheetAnswersAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
            {
                throw new UserFriendlyException("AI scoring is not enabled.");
            }

            var result = await applicationScoresheetAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion, capturePromptIo);
            if (string.Equals(result, "{}", StringComparison.Ordinal))
            {
                return result;
            }

            var application = await applicationRepository.GetAsync(applicationId);
            await localEventBus.PublishAsync(new AiScoresheetAnswersGeneratedEvent
            {
                Application = application
            });
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating AI scoresheet answers for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to regenerate AI scoresheet answers. Please try again.");
        }
    }
}
