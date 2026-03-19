using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.AttachmentSummary.AttachmentSummaryDefault)]
[Authorize(AIPermissions.ApplicationAnalysis.ApplicationAnalysisDefault)]
[Authorize(AIPermissions.ScoringAssistant.ScoringAssistantDefault)]
public class ApplicationAIContentAppService(
    IBackgroundJobManager backgroundJobManager,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationAIContentAppService
{
    public async Task<string> GenerateAIContentAsync(Guid applicationId, string? promptVersion = null)
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

            await backgroundJobManager.EnqueueAsync(new GenerateApplicationAIContentBackgroundJobArgs
            {
                ApplicationId = applicationId,
                PromptVersion = promptVersion,
                TenantId = CurrentTenant.Id
            });

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing full AI content pipeline for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI generate all. Please try again.");
        }
    }
}

