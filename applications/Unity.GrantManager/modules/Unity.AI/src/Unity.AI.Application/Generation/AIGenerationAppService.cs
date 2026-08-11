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
    [Authorize]
    [HttpPost("submit")]
    public virtual async Task SubmitAsync(string operationType, AIGenerationSubmissionDto request)
    {
        var operation = AIGenerationOperations.Get(operationType);
        await featureGuard.EnsureEnabledAsync(operation.FeatureName, operation.DisabledLocalizationKey);

        if (operation.RequiresFormVersion && request.ApplicationFormVersionId is null)
        {
            throw new UserFriendlyException($"AI operation '{operationType}' requires an application form version.");
        }

        await CheckPolicyAsync(operation.GeneratePermission);
        await aiGenerationQueue.QueueAsync(operationType, request, currentTenant.Id);
    }

    [Authorize(AIPermissions.Analysis.GenerateAttachmentSummaries)]
    [HttpPost("attachment-summary")]
    public virtual async Task GenerateApplicationAttachmentSummariesAsync(AttachmentSummaryGenerationRequestDto request)
    {
        await SubmitAsync(
            AIGenerationOperations.AttachmentSummary,
            new AIGenerationSubmissionDto
            {
                ApplicationId = request.ApplicationId,
                AttachmentIds = request.AttachmentIds,
                PromptVersion = request.PromptVersion
            });
    }

    [Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
    [HttpPost("application-analysis")]
    public virtual async Task GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        await SubmitAsync(
            AIGenerationOperations.ApplicationAnalysis,
            new AIGenerationSubmissionDto { ApplicationId = applicationId, PromptVersion = promptVersion });
    }

    [Authorize(AIPermissions.Analysis.GenerateScoring)]
    [HttpPost("application-scoring")]
    public virtual async Task GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        await SubmitAsync(
            AIGenerationOperations.ApplicationScoring,
            new AIGenerationSubmissionDto { ApplicationId = applicationId, PromptVersion = promptVersion });
    }

    [Authorize(AIPermissions.Analysis.GenerateFormMapping)]
    [HttpPost("form-mapping")]
    public virtual async Task GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await SubmitAsync(
            AIGenerationOperations.FormMapping,
            new AIGenerationSubmissionDto
            {
                ApplicationId = applicationId,
                ApplicationFormVersionId = applicationFormVersionId,
                PromptVersion = promptVersion
            });
    }

    [Authorize(AIPermissions.Analysis.GenerateFormWorksheet)]
    [HttpPost("form-worksheet")]
    public virtual async Task GenerateFormWorksheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await SubmitAsync(
            AIGenerationOperations.FormWorksheet,
            new AIGenerationSubmissionDto
            {
                ApplicationId = applicationId,
                ApplicationFormVersionId = applicationFormVersionId,
                PromptVersion = promptVersion
            });
    }

    [Authorize(AIPermissions.Analysis.GenerateFormScoresheet)]
    [HttpPost("form-scoresheet")]
    public virtual async Task GenerateFormScoresheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await SubmitAsync(
            AIGenerationOperations.FormScoresheet,
            new AIGenerationSubmissionDto
            {
                ApplicationId = applicationId,
                ApplicationFormVersionId = applicationFormVersionId,
                PromptVersion = promptVersion
            });
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
        if (!AIGenerationOperations.TryGet(operationType, out var operation))
        {
            throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}");
        }

        await CheckPolicyAsync(operation!.ViewPermission);
    }
}
