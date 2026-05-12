using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.ViewAttachmentSummary)]
[Authorize(AIPermissions.Analysis.ViewApplicationAnalysis)]
[Authorize(AIPermissions.Analysis.ViewScoringResult)]
public class ApplicationContentAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant)
    : AIAppService, IApplicationContentAppService
{
    public virtual async Task<ApplicationContentResultDto> GenerateContentAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(AIFeatures.AttachmentSummaries, AILocalizationKeys.GenerateAllDisabled);
        await featureGuard.EnsureEnabledAsync(AIFeatures.ApplicationAnalysis, AILocalizationKeys.GenerateAllDisabled);
        await featureGuard.EnsureEnabledAsync(AIFeatures.Scoring, AILocalizationKeys.GenerateAllDisabled);

        await aiGenerationQueue.QueueAllAIStagesAsync(applicationId, currentTenant.Id, promptVersion);

        return new ApplicationContentResultDto
        {
            Completed = true
        };
    }
}
