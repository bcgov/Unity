using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateScoring)]
public class ApplicationScoringAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    IFeatureChecker featureChecker)
    : AIAppService, IApplicationScoringAppService
{
    public async Task<string> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
            {
                throw new UserFriendlyException("AI scoring is not enabled.");
            }

            await aiGenerationQueue.QueueApplicationScoringAsync(applicationId, CurrentTenant.Id, promptVersion);

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing AI application scoring generation for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI application scoring generation. Please try again.");
        }
    }
}
