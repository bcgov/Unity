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

namespace Unity.AI.Generation;

[Route("api/app/ai/generation")]
public class AIGenerationAppService(
    IAttachmentSummaryService attachmentSummaryService,
    IApplicationAIGenerationQueue aiGenerationQueue,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant)
    : AIAppService, IAIGenerationAppService
{
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

        var summaries = await attachmentSummaryService.GenerateForApplicationAsync(
            input.ApplicationId,
            input.PromptVersion,
            input.AttachmentIds);

        return summaries.Select(_ => new AttachmentSummaryResultDto { Completed = true }).ToList();
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

    [Authorize(AIPermissions.Analysis.ViewAttachmentSummary)]
    [Authorize(AIPermissions.Analysis.ViewApplicationAnalysis)]
    [Authorize(AIPermissions.Analysis.ViewScoringResult)]
    [HttpPost("all")]
    public virtual async Task<ApplicationContentResultDto> GenerateContentAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(AIFeatures.AttachmentSummaries, AILocalizationKeys.GenerateAllDisabled);
        await featureGuard.EnsureEnabledAsync(AIFeatures.ApplicationAnalysis, AILocalizationKeys.GenerateAllDisabled);
        await featureGuard.EnsureEnabledAsync(AIFeatures.Scoring, AILocalizationKeys.GenerateAllDisabled);

        await aiGenerationQueue.QueueAllAIStagesAsync(applicationId, currentTenant.Id, promptVersion);

        return new ApplicationContentResultDto { Completed = true };
    }
}
