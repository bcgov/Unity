using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Models;
using Unity.AI.Prompts;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

public class AIApplicationInputBuilder(
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository,
    ILogger<AIApplicationInputBuilder> logger) : IAIApplicationInputBuilder, ITransientDependency
{
    public async Task<ApplicationAnalysisOperationInputDto> BuildApplicationAnalysisInputAsync(Guid applicationId, string? promptVersion)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);

        return new ApplicationAnalysisOperationInputDto
        {
            ApplicationId = applicationId,
            Schema = JsonSerializer.SerializeToElement(PromptDataPayloadBuilder.BuildFormFieldConfiguration(formSchema, logger)),
            Data = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger),
            Attachments = PromptDataPayloadBuilder.BuildAttachmentSummaries(attachments),
            PromptVersion = promptVersion
        };
    }

    public async Task<ApplicationScoringOperationInputDto> BuildApplicationScoringInputAsync(Guid applicationId, string? promptVersion)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);
        if (applicationForm.ScoresheetId == null)
        {
            throw new UserFriendlyException("Scoring requires a configured scoresheet.");
        }

        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
        {
            throw new UserFriendlyException("Scoring requires a scoresheet with fields.");
        }

        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        var attachmentSummaries = PromptDataPayloadBuilder.BuildAttachmentSummaries(
            attachments,
            excludeWhitespaceOnlySummaries: false);

        var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);
        var promptData = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger);

        var sections = scoresheet.Sections
            .OrderBy(s => s.Order)
            .Select(section => new ApplicationScoringSectionOperationInputDto
            {
                SectionName = section.Name,
                SectionSchema = JsonSerializer.SerializeToElement(BuildSectionQuestionsData(section))
            })
            .ToList();

        return new ApplicationScoringOperationInputDto
        {
            ApplicationId = applicationId,
            Data = promptData,
            Attachments = attachmentSummaries,
            Sections = sections,
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
            logger.LogWarning(ex, "Unable to load form schema for AI input generation for form version {FormVersionId}.", formVersionId);
            return null;
        }
    }

    private static List<object> BuildSectionQuestionsData(ScoresheetSection section)
    {
        var sectionQuestionsData = new List<object>();
        foreach (var field in section.Fields.OrderBy(f => f.Order))
        {
            var options = ExtractSelectListOptions(field);
            sectionQuestionsData.Add(new
            {
                id = field.Id.ToString(),
                section = section.Name,
                question = field.Label,
                description = field.Description,
                type = field.Type.ToString(),
                options,
                allowed_answers = ExtractSelectListOptionNumbers(options)
            });
        }

        return sectionQuestionsData;
    }

    private static object[]? ExtractSelectListOptions(Question field)
    {
        if (field.Type != Unity.Flex.Scoresheets.Enums.QuestionType.SelectList || string.IsNullOrEmpty(field.Definition))
        {
            return null;
        }

        try
        {
            var definition = JsonSerializer.Deserialize<Unity.Flex.Worksheets.Definitions.QuestionSelectListDefinition>(field.Definition);
            if (definition?.Options != null && definition.Options.Count > 0)
            {
                return definition.Options
                    .Select((option, index) =>
                        (object)new
                        {
                            number = index + 1,
                            value = option.Value
                        })
                    .ToArray();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed definition and return null options.
        }

        return null;
    }

    private static string[]? ExtractSelectListOptionNumbers(object[]? options)
    {
        if (options == null || options.Length == 0)
        {
            return null;
        }

        return options
            .Select((_, index) => (index + 1).ToString())
            .ToArray();
    }
}
