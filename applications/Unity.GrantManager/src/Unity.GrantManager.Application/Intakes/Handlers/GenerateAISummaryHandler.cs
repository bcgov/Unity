using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Unity.Flex.Domain.Scoresheets;
using System.Text.Json;
using Volo.Abp.Features;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class GenerateAiSummaryHandler : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        private readonly IAIService _aiService;
        private readonly ISubmissionAppService _submissionAppService;
        private readonly IApplicationChefsFileAttachmentRepository _attachmentRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly ILogger<GenerateAiSummaryHandler> _logger;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IFeatureChecker _featureChecker;

        readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        readonly JsonSerializerOptions jsonOptionsIndented = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public GenerateAiSummaryHandler(
            IAIService aiService,
            ISubmissionAppService submissionAppService,
            IApplicationChefsFileAttachmentRepository attachmentRepository,
            IApplicationRepository applicationRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ILogger<GenerateAiSummaryHandler> logger,
            IScoresheetRepository scoresheetRepository,
            IApplicationFormRepository applicationFormRepository,
            IFeatureChecker featureChecker)
        {
            _aiService = aiService;
            _submissionAppService = submissionAppService;
            _attachmentRepository = attachmentRepository;
            _applicationRepository = applicationRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _logger = logger;
            _scoresheetRepository = scoresheetRepository;
            _applicationFormRepository = applicationFormRepository;
            _featureChecker = featureChecker;
        }

        /// <summary>
        /// Generate AI summaries for attachments when a new application is processed
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            if (eventData?.Application == null)
            {
                _logger.LogWarning("Event data or application is null in GenerateAiSummaryHandler.");
                return;
            }

            // Check if either AI feature is enabled
            var attachmentSummariesEnabled = await _featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
            var applicationAnalysisEnabled = await _featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");

            if (!attachmentSummariesEnabled && !applicationAnalysisEnabled)
            {
                _logger.LogDebug("All AI features are disabled, skipping AI generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            // Check if AI service is available
            if (!await _aiService.IsAvailableAsync())
            {
                _logger.LogWarning("AI service is not available, skipping AI generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            _logger.LogInformation("Generating AI content for application {ApplicationId}.", eventData.Application.Id);

            try
            {
                // Get all CHEFS attachments for this application
                var attachments = await _attachmentRepository.GetListAsync(a => a.ApplicationId == eventData.Application.Id);

                // Generate attachment summaries if feature is enabled
                if (attachmentSummariesEnabled)
                {
                    foreach (var attachment in attachments)
                {
                    try
                    {
                        // Skip if already has an AI summary (don't regenerate)
                        if (!string.IsNullOrEmpty(attachment.AISummary))
                        {
                            _logger.LogDebug("Skipping AI summary for attachment {FileName} - already has summary", attachment.FileName);
                            continue;
                        }

                        _logger.LogDebug("Generating AI summary for attachment {FileName}", attachment.FileName);

                        try
                        {
                            // Get the file content from CHEFS (now accessible via [AllowAnonymous])
                            var fileDto = await _submissionAppService.GetChefsFileAttachment(
                                Guid.Parse(attachment.ChefsSumbissionId ?? ""),
                                Guid.Parse(attachment.ChefsFileId ?? ""),
                                attachment.FileName ?? "");

                            if (fileDto?.Content != null)
                            {
                                _logger.LogDebug("Processing {FileName} ({ContentType}, {Size} bytes) for AI summary generation",
                                    attachment.FileName, fileDto.ContentType, fileDto.Content.Length);

                                // Generate AI summary with text extraction and file content analysis
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName ?? "",
                                    fileDto.Content,
                                    fileDto.ContentType);

                                // Update the attachment with the AI summary
                                attachment.AISummary = summary;
                                await _attachmentRepository.UpdateAsync(attachment);

                                    var preview = summary is { Length: > 0 } s
                                    ? string.Concat(s.AsSpan(0, Math.Min(100, s.Length)), "...")
                                    : "...";

                                    _logger.LogDebug("Successfully generated AI summary for attachment {FileName}: {SummaryPreview}",
                                    attachment.FileName, preview);
                            }
                            else
                            {
                                _logger.LogWarning("Could not retrieve content for attachment {FileName}", attachment.FileName);

                                // Generate summary from filename only as fallback
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName ?? "",
                                    Array.Empty<byte>(),
                                    "application/octet-stream");

                                attachment.AISummary = summary;
                                await _attachmentRepository.UpdateAsync(attachment);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not access CHEFS file {FileName}. Generating summary from filename only.", attachment.FileName);

                            // Fallback: Generate summary from filename only
                            var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                attachment.FileName ?? "",
                                Array.Empty<byte>(),
                                "application/octet-stream");

                            attachment.AISummary = summary;
                            await _attachmentRepository.UpdateAsync(attachment);

                            _logger.LogDebug("Generated fallback AI summary for attachment {FileName} from filename only", attachment.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating AI summary for attachment {FileName} in application {ApplicationId}",
                            attachment.FileName, eventData.Application.Id);
                        // Continue processing other attachments even if one fails
                    }
                }
                }

                // Generate application analysis and scoresheet if feature is enabled
                if (applicationAnalysisEnabled)
                {
                    // After processing all attachments, perform application analysis
                    await GenerateApplicationAnalysisAsync(eventData.Application, attachments);

                    // Generate AI scoresheet answers
                    await GenerateScoresheetAnalysisAsync(eventData.Application, attachments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI content for application {ApplicationId}", eventData.Application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private async Task GenerateApplicationAnalysisAsync(Application application, List<ApplicationChefsFileAttachment> attachments)
        {
            try
            {
                _logger.LogDebug("Starting application analysis for {ApplicationId}", application.Id);

                // Load the application from repository to ensure proper change tracking
                var trackedApplication = await _applicationRepository.GetAsync(application.Id);

                // Skip if application already has analysis
                if (!string.IsNullOrEmpty(trackedApplication.AIAnalysis))
                {
                    _logger.LogDebug("Skipping application analysis for {ApplicationId} - already has analysis", application.Id);
                    return;
                }

                // Collect all attachment summaries that were generated
                var attachmentSummaries = attachments
                    .Where(a => !string.IsNullOrEmpty(a.AISummary))
                    .Select(a => $"{a.FileName}: {a.AISummary}")
                    .ToList();

                // Get form submission content including rendered HTML
                var formSubmission = await _applicationFormSubmissionRepository
                    .GetByApplicationAsync(application.Id);

                // Get application content including the full form submission
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
                _logger.LogInformation("Generating analysis for following application: {Application}", applicationContent);

                // Hardcoded rubric for now
                var rubric = @"
BC GOVERNMENT GRANT EVALUATION RUBRIC:

1. ELIGIBILITY REQUIREMENTS:
   - Project must align with program objectives
   - Applicant must be eligible entity type
   - Budget must be reasonable and well-justified
   - Project timeline must be realistic

2. COMPLETENESS CHECKS:
   - All required fields completed
   - Necessary supporting documents provided
   - Budget breakdown detailed and accurate
   - Project description clear and comprehensive

3. FINANCIAL REVIEW:
   - Requested amount is within program limits
   - Budget is reasonable for scope of work
   - Matching funds or in-kind contributions identified
   - Cost per outcome/beneficiary is reasonable

4. RISK ASSESSMENT:
   - Applicant capacity to deliver project
   - Technical feasibility of proposed work
   - Environmental or regulatory compliance
   - Potential for cost overruns or delays

5. QUALITY INDICATORS:
   - Clear project objectives and outcomes
   - Well-defined target audience/beneficiaries
   - Appropriate project methodology
   - Sustainability plan for long-term impact

EVALUATION CRITERIA:
- HIGH: Meets all requirements, well-prepared application, low risk
- MEDIUM: Meets most requirements, minor issues or missing elements
- LOW: Missing key requirements, significant concerns, high risk
";

                _logger.LogDebug("Generating AI analysis for application {ApplicationId} with {AttachmentCount} attachment summaries",
                    application.Id, attachmentSummaries.Count);

                // Generate the analysis
                var analysis = await _aiService.AnalyzeApplicationAsync(applicationContent, attachmentSummaries, rubric);

                // Clean the response to remove any markdown formatting
                var cleanedAnalysis = CleanJsonResponse(analysis);

                // Update the tracked application with the analysis
                trackedApplication.AIAnalysis = cleanedAnalysis;
                await _applicationRepository.UpdateAsync(trackedApplication);

                _logger.LogInformation("Successfully generated AI analysis for application {ApplicationId}", application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application analysis for {ApplicationId}", application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private async Task GenerateScoresheetAnalysisAsync(Application application, List<ApplicationChefsFileAttachment> attachments)
        {
            try
            {
                _logger.LogDebug("Starting scoresheet analysis for {ApplicationId}", application.Id);

                // Load the application from repository to ensure proper change tracking
                var trackedApplication = await _applicationRepository.GetAsync(application.Id);

                // Skip if application already has scoresheet analysis
                if (!string.IsNullOrEmpty(trackedApplication.AIScoresheetAnswers))
                {
                    _logger.LogDebug("Skipping scoresheet analysis for {ApplicationId} - already has scoresheet answers", application.Id);
                    return;
                }

                // Get the scoresheet for this application's form (using direct relationship like AssessmentManager does)
                _logger.LogDebug("Getting ApplicationForm for application {ApplicationId} with ApplicationFormId {ApplicationFormId}",
                    application.Id, application.ApplicationFormId);

                var applicationForm = await _applicationFormRepository.GetAsync(application.ApplicationFormId);
                if (applicationForm == null)
                {
                    _logger.LogDebug("ApplicationForm not found with ID {ApplicationFormId} for application {ApplicationId}",
                        application.ApplicationFormId, application.Id);
                    return;
                }

                _logger.LogDebug("Found ApplicationForm {ApplicationFormName} with ScoresheetId {ScoresheetId} for application {ApplicationId}",
                    applicationForm.ApplicationFormName, applicationForm.ScoresheetId, application.Id);

                if (applicationForm.ScoresheetId == null)
                {
                    _logger.LogDebug("No scoresheet found for application {ApplicationId} - ApplicationForm {ApplicationFormId} has null ScoresheetId",
                        application.Id, application.ApplicationFormId);
                    return;
                }

                var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
                if (scoresheet == null)
                {
                    _logger.LogDebug("Scoresheet not found for application {ApplicationId}", application.Id);
                    return;
                }

                // Process each section individually for better AI focus
                var allSectionResults = new Dictionary<string, object>();

                // Collect all attachment summaries that were generated
                var attachmentSummaries = attachments
                    .Where(a => !string.IsNullOrEmpty(a.AISummary))
                    .Select(a => $"{a.FileName}: {a.AISummary}")
                    .ToList();

                // Get form submission for rendered HTML content
                var formSubmission = await _applicationFormSubmissionRepository.GetByApplicationAsync(application.Id);

                // Get application content including the full form submission
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

                _logger.LogInformation("Form submission HTML length: {HtmlLength} characters", formSubmission?.RenderedHTML?.Length ?? 0);
                if (formSubmission?.RenderedHTML?.Length > 100)
                {
                    _logger.LogDebug("Form submission HTML preview: {HtmlPreview}...",
                        formSubmission.RenderedHTML.Substring(0, Math.Min(500, formSubmission.RenderedHTML.Length)));
                }
                else
                {
                    _logger.LogWarning("Form submission HTML is missing or very short: {FullHtml}", formSubmission?.RenderedHTML);
                }

                // Process each section individually
                foreach (var section in scoresheet.Sections.OrderBy(s => s.Order))
                {
                    try
                    {
                        _logger.LogDebug("Processing section {SectionName} for application {ApplicationId}",
                            section.Name, application.Id);

                        // Build section-specific JSON
                        var sectionQuestionsData = new List<object>();
                        foreach (var field in section.Fields.OrderBy(f => f.Order))
                        {
                            var questionData = new
                            {
                                id = field.Id.ToString(),
                                question = field.Label,
                                description = field.Description,
                                type = field.Type.ToString(),
                                definition = field.Definition,
                                availableOptions = ExtractSelectListOptions(field)
                            };
                            sectionQuestionsData.Add(questionData);
                        }

                        var sectionJson = JsonSerializer.Serialize(sectionQuestionsData, jsonOptions);

                        // Generate AI answers for this section
                        var sectionAnswers = await _aiService.GenerateScoresheetSectionAnswersAsync(
                            applicationContent,
                            attachmentSummaries,
                            sectionJson,
                            section.Name);

                        // Parse and store section results
                        if (!string.IsNullOrWhiteSpace(sectionAnswers))
                        {
                            var cleanedJson = CleanJsonResponse(sectionAnswers);
                            try
                            {
                                using var sectionDoc = JsonDocument.Parse(cleanedJson);
                                foreach (var property in sectionDoc.RootElement.EnumerateObject())
                                {
                                    allSectionResults[property.Name] = property.Value.Clone();
                                }

                                _logger.LogDebug("Successfully processed section {SectionName} with {QuestionCount} questions",
                                    section.Name, sectionQuestionsData.Count);
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse AI response for section {SectionName} in application {ApplicationId}. Content: {InvalidJson}",
                                    section.Name, application.Id, sectionAnswers);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing section {SectionName} for application {ApplicationId}",
                            section.Name, application.Id);
                        // Continue with other sections even if one fails
                    }
                }

                // Combine all section results into final JSON
                var combinedResults = JsonSerializer.Serialize(allSectionResults, jsonOptionsIndented);

                var scoresheetAnswers = combinedResults;

                // Validate and sanitize the JSON before saving
                string validatedJson = "{}"; // Default empty JSON
                try
                {
                    if (!string.IsNullOrWhiteSpace(scoresheetAnswers))
                    {
                        // Try to parse the JSON to validate it
                        using var jsonDoc = JsonDocument.Parse(scoresheetAnswers);
                        validatedJson = scoresheetAnswers;
                        _logger.LogDebug("AI generated valid JSON for scoresheet answers: {JsonPreview}",
                            scoresheetAnswers);
                    }
                    else
                    {
                        _logger.LogWarning("AI service returned empty or null scoresheet answers for application {ApplicationId}", application.Id);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "AI service returned invalid JSON for scoresheet answers for application {ApplicationId}. Content: {InvalidJson}",
                        application.Id, scoresheetAnswers);
                    validatedJson = "{}"; // Use empty JSON as fallback
                }

                // Store AI scoresheet answers in the application for later parsing
                trackedApplication.AIScoresheetAnswers = validatedJson;
                await _applicationRepository.UpdateAsync(trackedApplication);

                _logger.LogInformation("Successfully generated and saved AI scoresheet answers for application {ApplicationId}. Answers will be parsed when scoresheet instance is created.", application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet analysis for {ApplicationId}", application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private static string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            // Remove markdown code block delimiters
            var cleaned = response.Trim();

            // Handle ```json opening tag
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || cleaned.StartsWith("```"))
            {
                var startIndex = cleaned.IndexOf('\n');
                if (startIndex >= 0)
                {
                    cleaned = cleaned.Substring(startIndex + 1);
                }
            }

            // Handle closing ``` tag
            if (cleaned.EndsWith("```"))
            {
                var lastIndex = cleaned.LastIndexOf("```");
                if (lastIndex > 0)
                {
                    cleaned = cleaned.Substring(0, lastIndex);
                }
            }

            return cleaned.Trim();
        }

        private static (int number, string value, long numericValue)[]? ExtractSelectListOptions(Unity.Flex.Domain.Scoresheets.Question field)
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
                            (number: index,
                             value: option.Value,
                             numericValue: option.NumericValue))
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                // If definition parsing fails, return null
            }

            return null;
        }

    }
}
