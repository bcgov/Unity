using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.AI.Generation;

[Route("api/app/ai/generation")]
public class AIGenerationAppService(
    IApplicationGenerationQueue aiGenerationQueue,
    IAIGenerationStatusAppService aiGenerationStatusAppService,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant)
    : AIAppService, IAIGenerationAppService
{
    [Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
    [HttpPost("attachment-summary")]
    public virtual async Task GenerateApplicationAttachmentSummariesAsync(AttachmentSummaryGenerationRequestDto request)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.AttachmentSummaries,
            AILocalizationKeys.AttachmentSummariesDisabled);

        if (request.AttachmentIds.Count == 0)
        {
            return;
        }

        await aiGenerationQueue.QueueApplicationAttachmentSummaryAsync(
            request.ApplicationId,
            currentTenant.Id,
            request.AttachmentIds,
            request.PromptVersion);
    }

    [Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
    [HttpPost("application-analysis")]
    public virtual async Task GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.ApplicationAnalysis,
            AILocalizationKeys.ApplicationAnalysisDisabled);

        await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, currentTenant.Id, promptVersion);
    }

    [Authorize(AIPermissions.Analysis.GenerateScoring)]
    [HttpPost("application-scoring")]
    public virtual async Task GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.Scoring,
            AILocalizationKeys.ScoringDisabled);

        await aiGenerationQueue.QueueApplicationScoringAsync(applicationId, currentTenant.Id, promptVersion);
    }

    [Authorize(AIPermissions.Analysis.GenerateFormMapping)]
    [HttpPost("form-mapping")]
    public virtual async Task GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormMapping,
            AILocalizationKeys.FormMappingDisabled);

        await aiGenerationQueue.QueueFormMappingAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
    }

    [Authorize]
    [HttpGet("status")]
    public virtual async Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType)
    {
        await EnsureStatusAccessAsync(operationType);

        var request = await aiGenerationStatusAppService.GetLatestAsync(applicationId, operationType, currentTenant.Id);
        if (request == null)
        {
            return new AIGenerationStatusDto();
        }

        return new AIGenerationStatusDto
        {
            GenerationRequest = new AIGenerationRequestDto
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
            Id = request.Id,
            ApplicationId = request.ApplicationId,
            OperationId = request.OperationId,
            OperationType = operationType,
            Status = request.Status.ToString(),
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
            FailureReason = request.FailureReason,
            IsActive = request.IsActive
        };
    }

    private async Task EnsureStatusAccessAsync(string operationType)
    {
        var permission = operationType switch
        {
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType => AIPermissions.Analysis.ViewApplicationAnalysis,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType => AIPermissions.Analysis.ViewAttachmentSummary,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType => AIPermissions.Analysis.ViewScoringResult,
            AIGenerationRequestKeyHelper.FormMappingOperationType => AIPermissions.Analysis.ViewFormMapping,
            _ => throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}")
        };

        await CheckPolicyAsync(permission);
    }
}
