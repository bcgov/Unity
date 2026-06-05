using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Prompts;
using Unity.AI.Requests;
using Unity.AI.Runtime;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations
{
    public class ApplicationAnalysisService(
        IApplicationRepository applicationRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        IAIService aiService,
        IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
        ILogger<ApplicationAnalysisService> logger) : IApplicationAnalysisService, ITransientDependency
    {
        public async Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null, CancellationToken cancellationToken = default)
        {
            await aiGenerationPrerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(applicationId);

            var application = await applicationRepository.GetAsync(applicationId);
            var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
            var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
            var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);

            var attachmentSummaries = PromptDataPayloadBuilder.BuildAttachmentSummaries(attachments);
            var formFieldConfiguration = await PromptDataPayloadBuilder.BuildFormFieldConfigurationAsync(
                applicationFormVersionRepository,
                formSubmission?.ApplicationFormVersionId,
                logger);

            var analysis = await aiService.GenerateApplicationAnalysisAsync(new ApplicationAnalysisRequest
            {
                Schema = JsonSerializer.SerializeToElement(formFieldConfiguration),
                Data = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger),
                Attachments = attachmentSummaries,
                PromptVersion = promptVersion,
            }, cancellationToken);

            var analysisJson = JsonSerializer.Serialize(analysis, AIJsonDefaults.Indented);
            application.AIAnalysis = analysisJson;
            await applicationRepository.UpdateAsync(application);
            return analysisJson;
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
}
