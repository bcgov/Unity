using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class ApplicationAIScoringService(
        IApplicationRepository applicationRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        IScoresheetRepository scoresheetRepository,
        IAIService aiService,
        ILogger<ApplicationAIScoringService> logger) : IApplicationAIScoringService, ITransientDependency
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly JsonSerializerOptions _jsonOptionsIndented = new()
        {
            WriteIndented = true
        };

        public async Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null)
        {
            var application = await applicationRepository.GetAsync(applicationId);
            var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);
            if (applicationForm.ScoresheetId == null)
            {
                return "{}";
            }

            var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
            if (scoresheet == null)
            {
                return "{}";
            }

            var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
            var attachmentSummaries = attachments
                .Where(a => !string.IsNullOrEmpty(a.AISummary))
                .Select(a => new AIAttachmentItem
                {
                    Name = string.IsNullOrWhiteSpace(a.FileName) ? "attachment" : a.FileName.Trim(),
                    Summary = a.AISummary!.Trim()
                })
                .ToList();

            var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
            var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);
            var promptData = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger);

            var allSectionResults = new Dictionary<string, object>();
            foreach (var section in scoresheet.Sections.OrderBy(s => s.Order))
            {
                try
                {
                    var sectionQuestionsData = new List<object>();
                    foreach (var field in section.Fields.OrderBy(f => f.Order))
                    {
                        var options = ExtractSelectListOptions(field);
                        sectionQuestionsData.Add(new
                        {
                            id = field.Id.ToString(),
                            question = field.Label,
                            description = field.Description,
                            type = field.Type.ToString(),
                            options,
                            allowed_answers = ExtractSelectListOptionNumbers(options)
                        });
                    }

                    var sectionRequest = new ScoresheetSectionRequest
                    {
                        Data = promptData,
                        Attachments = attachmentSummaries,
                        SectionName = section.Name,
                        SectionSchema = JsonSerializer.SerializeToElement(sectionQuestionsData, _jsonOptions),
                        PromptVersion = promptVersion,
                    };
                    var sectionAnswers = await aiService.GenerateScoresheetSectionAsync(sectionRequest);

                    if (sectionAnswers.Answers.Count > 0)
                    {
                        var sectionJson = JsonSerializer.Serialize(sectionAnswers.Answers, _jsonOptions);
                        using var sectionDoc = JsonDocument.Parse(sectionJson);
                        foreach (var property in sectionDoc.RootElement.EnumerateObject())
                        {
                            allSectionResults[property.Name] = property.Value.Clone();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing AI scoresheet section {SectionName} for application {ApplicationId}", section.Name, applicationId);
                }
            }

            var combinedResults = JsonSerializer.Serialize(allSectionResults, _jsonOptionsIndented);
            var validatedJson = ValidateScoresheetJson(combinedResults);
            application.AIScoresheetAnswers = validatedJson;
            await applicationRepository.UpdateAsync(application);
            return validatedJson;
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
                logger.LogWarning(ex, "Unable to load form schema for scoresheet prompt data generation for form version {FormVersionId}.", formVersionId);
                return null;
            }
        }

        private static string ValidateScoresheetJson(string scoresheetAnswers)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(scoresheetAnswers))
                {
                    using var _ = JsonDocument.Parse(scoresheetAnswers);
                    return scoresheetAnswers;
                }
            }
            catch (JsonException)
            {
                // Fall back to empty object for invalid JSON.
            }

            return "{}";
        }

        private static object[]? ExtractSelectListOptions(Question field)
        {
            if (field.Type != Unity.Flex.Scoresheets.Enums.QuestionType.SelectList || string.IsNullOrEmpty(field.Definition))
                return null;

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
}


