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

    [Authorize(AIPermissions.Analysis.GenerateFormMapping)]
    [HttpPost("form-mapping")]
    public virtual async Task<FormMappingResultDto> GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormMapping,
            AILocalizationKeys.FormMappingDisabled);

        await aiGenerationQueue.QueueFormMappingAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
        return new FormMappingResultDto { Completed = false };
    }

    [Authorize(AIPermissions.Analysis.GenerateFormWorksheet)]
    [HttpPost("form-worksheet")]
    public virtual async Task<FormWorksheetResultDto> GenerateFormWorksheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormWorksheet,
            AILocalizationKeys.FormWorksheetDisabled);

        await aiGenerationQueue.QueueFormWorksheetAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
        return new FormWorksheetResultDto { Completed = false };
    }

    [Authorize(AIPermissions.Analysis.GenerateFormScoresheet)]
    [HttpPost("form-scoresheet")]
    public virtual async Task<FormScoresheetResultDto> GenerateFormScoresheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.FormScoresheet,
            AILocalizationKeys.FormScoresheetDisabled);

        await aiGenerationQueue.QueueFormScoresheetAsync(applicationId, currentTenant.Id, applicationFormVersionId, promptVersion);
        return new FormScoresheetResultDto { Completed = false };
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
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType => AIPermissions.Analysis.ViewApplicationAnalysis,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType => AIPermissions.Analysis.ViewAttachmentSummary,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType => AIPermissions.Analysis.ViewScoringResult,
            AIGenerationRequestKeyHelper.FormMappingOperationType => AIPermissions.Analysis.ViewFormMapping,
            AIGenerationRequestKeyHelper.FormWorksheetOperationType => AIPermissions.Analysis.ViewFormWorksheet,
            AIGenerationRequestKeyHelper.FormScoresheetOperationType => AIPermissions.Analysis.ViewFormScoresheet,
            _ => throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}")
        };

        await CheckPolicyAsync(permission);
    }
}
