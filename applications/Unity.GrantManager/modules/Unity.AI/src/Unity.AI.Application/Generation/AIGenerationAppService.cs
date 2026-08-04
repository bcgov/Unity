using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.AI.Generation;

[Route("api/app/ai/generation")]
public class AIGenerationAppService(
    IApplicationGenerationQueue aiGenerationQueue,
    IAIGenerationStatusReader aiGenerationStatusReader,
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

    [Authorize(AIPermissions.Analysis.GenerateFormWorksheet)]
    [HttpPost("form-worksheet")]
    public virtual async Task GenerateFormWorksheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormWorksheet,
            AILocalizationKeys.FormWorksheetDisabled);

        await aiGenerationQueue.QueueFormWorksheetAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
    }

    [Authorize(AIPermissions.Analysis.GenerateFormScoresheet)]
    [HttpPost("form-scoresheet")]
    public virtual async Task GenerateFormScoresheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormScoresheet,
            AILocalizationKeys.FormScoresheetDisabled);

        await aiGenerationQueue.QueueFormScoresheetAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
    }

    [Authorize]
    [HttpGet("status")]
    public virtual async Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType)
    {
        await EnsureStatusAccessAsync(operationType);

        var request = await aiGenerationStatusReader.GetLatestAsync(applicationId, operationType, currentTenant.Id);
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
            AIGenerationOperationKeyHelper.ApplicationAnalysisOperationType => AIPermissions.Analysis.ViewApplicationAnalysis,
            AIGenerationOperationKeyHelper.AttachmentSummaryOperationType => AIPermissions.Analysis.ViewAttachmentSummary,
            AIGenerationOperationKeyHelper.ApplicationScoringOperationType => AIPermissions.Analysis.ViewScoringResult,
            AIGenerationOperationKeyHelper.FormMappingOperationType => AIPermissions.Analysis.ViewFormMapping,
            AIGenerationOperationKeyHelper.FormWorksheetOperationType => AIPermissions.Analysis.ViewFormWorksheet,
            AIGenerationOperationKeyHelper.FormScoresheetOperationType => AIPermissions.Analysis.ViewFormScoresheet,
            _ => throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}")
        };

        await CheckPolicyAsync(permission);
    }
}
