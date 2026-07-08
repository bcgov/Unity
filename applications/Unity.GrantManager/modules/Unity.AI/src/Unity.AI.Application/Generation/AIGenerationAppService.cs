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
using Unity.AI.RateLimit;
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
    IAIGenerationStatusAppService aiGenerationStatusAppService,
    IAIRateLimiter aiRateLimiter,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant)
    : AIAppService, IAIGenerationAppService
{
    private const string ApplicationAnalysisOperationType = "application-analysis";
    private const string AttachmentSummaryOperationType = "attachment-summary";
    private const string ApplicationScoringOperationType = "application-scoring";

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

    [Authorize]
    [HttpGet("status")]
    public virtual async Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType)
    {
        await EnsureStatusAccessAsync(operationType);

        var request = await aiGenerationStatusAppService.GetLatestAsync(applicationId, operationType, currentTenant.Id);
        var state = await aiRateLimiter.GetStateAsync();

        return new AIGenerationStatusDto
        {
            GenerationRequest = request == null
                ? null
                : new AIGenerationStatusRequestDto
                {
                    Id = request.Id,
                    ApplicationId = request.ApplicationId,
                    OperationId = request.OperationId,
                    OperationType = operationType,
                    Status = request.Status.ToString(),
                    StartedAt = request.StartedAt,
                    CompletedAt = request.CompletedAt,
                    FailureReason = request.FailureReason,
                    IsActive = request.IsActive
                },
            IsGenerating = state.IsGenerating,
            RetryAfterSeconds = state.RetryAfterSeconds
        };
    }

    private async Task EnsureStatusAccessAsync(string operationType)
    {
        var permission = operationType switch
        {
            ApplicationAnalysisOperationType => AIPermissions.Analysis.ViewApplicationAnalysis,
            AttachmentSummaryOperationType => AIPermissions.Analysis.ViewAttachmentSummary,
            ApplicationScoringOperationType => AIPermissions.Analysis.ViewScoringResult,
            AIGenerationRequestKeyHelper.PipelineOperationType => null,
            _ => throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}")
        };

        if (permission is null)
        {
            await CheckPolicyAsync(AIPermissions.Analysis.ViewApplicationAnalysis);
            await CheckPolicyAsync(AIPermissions.Analysis.ViewAttachmentSummary);
            await CheckPolicyAsync(AIPermissions.Analysis.ViewScoringResult);
            return;
        }

        await CheckPolicyAsync(permission);
    }
}
