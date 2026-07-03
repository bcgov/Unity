using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.AI.Generation;

[Route("api/app/ai/generation")]
public class AIGenerationAppService(
    IApplicationAIGenerationQueue aiGenerationQueue,
    AIFeatureGuard featureGuard,
    IFeatureChecker featureChecker,
    ICurrentTenant currentTenant)
    : AIAppService, IAIGenerationAppService
{
    private readonly IFeatureChecker _featureChecker = featureChecker;

    [Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
    [HttpPost("attachment-summary")]
    public virtual async Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(GenerateAttachmentSummariesInputDto input)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.AttachmentSummaries,
            AILocalizationKeys.AttachmentSummariesDisabled);

        if (input.AttachmentIds.Count == 0)
        {
            return [];
        }

        await aiGenerationQueue.QueueAttachmentSummaryAsync(
            input.ApplicationId,
            currentTenant.Id,
            input.PromptVersion,
            input.AttachmentIds);

        return input.AttachmentIds
            .Select(_ => new AttachmentSummaryResultDto { Completed = false })
            .ToList();
    }

    [Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
    [HttpPost("application-analysis")]
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.ApplicationAnalysis,
            AILocalizationKeys.ApplicationAnalysisDisabled);

        await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, currentTenant.Id, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = false };
    }

    [Authorize(AIPermissions.Analysis.GenerateScoring)]
    [HttpPost("application-scoring")]
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.Scoring,
            AILocalizationKeys.ScoringDisabled);

        await aiGenerationQueue.QueueApplicationScoringAsync(applicationId, currentTenant.Id, promptVersion);
        return new ApplicationScoringResultDto { Completed = false };
    }

    [Authorize(AIPermissions.Analysis.GenerateAll)]
    [HttpPost("all")]
    public virtual async Task<ApplicationContentResultDto> GenerateContentAsync(Guid applicationId, string? promptVersion = null)
    {
        var hasQueuedStage = false;

        if (await _featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries))
        {
            await aiGenerationQueue.QueueAttachmentSummaryAsync(applicationId, currentTenant.Id, promptVersion);
            hasQueuedStage = true;
        }

        if (await _featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis))
        {
            await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, currentTenant.Id, promptVersion);
            hasQueuedStage = true;
        }

        if (await _featureChecker.IsEnabledAsync(AIFeatures.Scoring))
        {
            await aiGenerationQueue.QueueApplicationScoringAsync(applicationId, currentTenant.Id, promptVersion);
            hasQueuedStage = true;
        }

        if (!hasQueuedStage)
        {
            throw new UserFriendlyException(L[AILocalizationKeys.GenerateAllDisabled]);
        }

        return new ApplicationContentResultDto { Completed = false };
    }
}
