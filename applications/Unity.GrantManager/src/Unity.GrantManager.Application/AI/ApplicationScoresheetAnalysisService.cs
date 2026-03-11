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
    public class ApplicationScoresheetAnalysisService(
        IApplicationRepository applicationRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        IScoresheetRepository scoresheetRepository,
        IAIService aiService,
        ILogger<ApplicationScoresheetAnalysisService> logger) : IApplicationScoresheetAnalysisService, ITransientDependency
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

        public async Task<string> RegenerateAndSaveAsync(Guid applicationId)
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
            var notSpecified = "Not specified";
            var applicationContent = $@"
Project Name: {application.ProjectName}
Reference Number: {application.ReferenceNo}
Requested Amount: ${application.RequestedAmount:N2}
Total Project Budget: ${application.TotalProjectBudget:N2}
Project Summary: {application.ProjectSummary ?? "Not provided"}
City: {application.City ?? notSpecified}
Economic Region: {application.EconomicRegion ?? notSpecified}
Community: {application.Community ?? notSpecified}
Project Start Date: {application.ProjectStartDate?.ToShortDateString() ?? notSpecified}
Project End Date: {application.ProjectEndDate?.ToShortDateString() ?? notSpecified}
Submission Date: {application.SubmissionDate.ToShortDateString()}

FULL APPLICATION FORM SUBMISSION:
{formSubmission?.RenderedHTML ?? "Form submission content not available"}
";

            var allSectionResults = new Dictionary<string, object>();
            foreach (var section in scoresheet.Sections.OrderBy(s => s.Order))
            {
                try
                {
                    var sectionQuestionsData = new List<object>();
                    foreach (var field in section.Fields.OrderBy(f => f.Order))
                    {
                        sectionQuestionsData.Add(new
                        {
                            id = field.Id.ToString(),
                            question = field.Label,
                            description = field.Description,
                            type = field.Type.ToString(),
                            definition = field.Definition,
                            availableOptions = ExtractSelectListOptions(field)
                        });
                    }

                    var sectionRequest = new ScoresheetSectionRequest
                    {
                        Data = JsonSerializer.SerializeToElement(new { submission_content = applicationContent }),
                        Attachments = attachmentSummaries,
                        SectionName = section.Name,
                        SectionSchema = JsonSerializer.SerializeToElement(sectionQuestionsData, _jsonOptions)
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

        private static (int number, string value, long numericValue)[]? ExtractSelectListOptions(Question field)
        {
            if (field.Type != Unity.Flex.Scoresheets.Enums.QuestionType.SelectList || string.IsNullOrEmpty(field.Definition))
                return null;

            try
            {
                var definition = JsonSerializer.Deserialize<Unity.Flex.Worksheets.Definitions.QuestionSelectListDefinition>(field.Definition);
                if (definition?.Options != null && definition.Options.Count > 0)
                {
                    return definition.Options
                        .Select((option, index) => (number: index, value: option.Value, numericValue: option.NumericValue))
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                // Ignore malformed definition and return null options.
            }

            return null;
        }
    }
}
