using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Permissions;
using Unity.AI.Automation;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
public class ApplicationAnalysisAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    IFeatureChecker featureChecker)
    : AIAppService, IApplicationAnalysisAppService
{
    public async Task<string> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis"))
            {
                throw new UserFriendlyException("AI application analysis is not enabled.");
            }

            await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, CurrentTenant.Id, promptVersion);

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing AI analysis for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI analysis. Please try again.");
        }
    }
}
