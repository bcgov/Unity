using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.ViewAttachmentSummary)]
[Authorize(AIPermissions.Analysis.ViewApplicationAnalysis)]
[Authorize(AIPermissions.Analysis.ViewScoringResult)]
public class ApplicationContentAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    IFeatureChecker featureChecker,
    ICurrentTenant currentTenant)
    : AIAppService, IApplicationContentAppService
{
    public async Task<ApplicationContentResultDto> GenerateContentAsync(Guid applicationId, string? promptVersion = null)
    {
        var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
        var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
        var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

        if (!attachmentSummariesEnabled || !applicationAnalysisEnabled || !scoringEnabled)
        {
            throw new UserFriendlyException("AI generate all is not enabled.");
        }

        await aiGenerationQueue.QueueApplicationPipelineAsync(applicationId, currentTenant.Id, promptVersion);

        return new ApplicationContentResultDto
        {
            Completed = true
        };
    }
}
