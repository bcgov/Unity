using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Models;
using Unity.AI.Prompts;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
public class ApplicationAnalysisAppService(
    Unity.AI.Operations.IApplicationAnalysisService applicationAnalysisService,
    IApplicationAIGenerationQueue aiGenerationQueue,
    IApplicationRepository applicationRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant,
    ILogger<ApplicationAnalysisAppService> logger)
    : AIAppService, IApplicationAnalysisAppService
{
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.ApplicationAnalysis,
            AILocalizationKeys.ApplicationAnalysisDisabled);

        await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, currentTenant.Id, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = false };
    }

    // Internal-only: no HTTP endpoint, no auth check — safe for background job callers
    [AllowAnonymous]
    [RemoteService(IsEnabled = false)]
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisForPipelineAsync(Guid applicationId, string? promptVersion = null)
    {
        var input = await BuildInputAsync(applicationId, promptVersion);
        var analysisJson = await applicationAnalysisService.RegenerateAsync(input);
        var application = await applicationRepository.GetAsync(applicationId);
        application.AIAnalysis = analysisJson;
        await applicationRepository.UpdateAsync(application);
        return new ApplicationAnalysisResultDto { Completed = true };
    }

    private async Task<Unity.AI.Operations.ApplicationAnalysisOperationInputDto> BuildInputAsync(Guid applicationId, string? promptVersion)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);

        var attachmentSummaries = PromptDataPayloadBuilder.BuildAttachmentSummaries(attachments);
        var formFieldConfiguration = PromptDataPayloadBuilder.BuildFormFieldConfigurationAsync(
            formSchema,
            logger);

        return new Unity.AI.Operations.ApplicationAnalysisOperationInputDto
        {
            ApplicationId = applicationId,
            Schema = JsonSerializer.SerializeToElement(formFieldConfiguration),
            Data = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger),
            Attachments = attachmentSummaries,
            PromptVersion = promptVersion
        };
    }

    private async Task<string?> GetFormSchemaAsync(Guid? formVersionId)
    {
        if (formVersionId == null)
        {
            return null;
        }

        try
        {
            var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId.Value);
            return string.IsNullOrWhiteSpace(formVersion?.FormSchema) ? null : formVersion.FormSchema;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to load form schema for prompt data generation for form version {FormVersionId}.", formVersionId);
            return null;
        }
    }
}
