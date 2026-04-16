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

[Authorize(AIPermissions.Analysis.ViewAttachmentSummary)]
[Authorize(AIPermissions.Analysis.ViewApplicationAnalysis)]
[Authorize(AIPermissions.Analysis.ViewScoringResult)]
public class ApplicationContentAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    IFeatureChecker featureChecker)
    : AIAppService, IApplicationContentAppService
{
    public async Task<string> GenerateContentAsync(Guid applicationId, string? promptVersion = null)
    {
        try
        {
            var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
            var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
            var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

            if (!attachmentSummariesEnabled || !applicationAnalysisEnabled || !scoringEnabled)
            {
                throw new UserFriendlyException("AI generate all is not enabled.");
            }

            await aiGenerationQueue.QueueApplicationPipelineAsync(applicationId, CurrentTenant.Id, promptVersion);

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing full AI content pipeline for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI generate all. Please try again.");
        }
    }
}
