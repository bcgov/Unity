using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Events;
using Unity.Modules.Shared.Features;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Scoresheets;
using System.Text.Json;

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
        private readonly IFeatureChecker _featureChecker;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IScoresheetInstanceRepository _scoresheetInstanceRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;

        public GenerateAiSummaryHandler(
            IAIService aiService,
            ISubmissionAppService submissionAppService,
            IApplicationChefsFileAttachmentRepository attachmentRepository,
            IApplicationRepository applicationRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ILogger<GenerateAiSummaryHandler> logger,
            IFeatureChecker featureChecker,
            IScoresheetRepository scoresheetRepository,
            IScoresheetInstanceRepository scoresheetInstanceRepository,
            IApplicationFormRepository applicationFormRepository)
        {
            _aiService = aiService;
            _submissionAppService = submissionAppService;
            _attachmentRepository = attachmentRepository;
            _applicationRepository = applicationRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _logger = logger;
            _featureChecker = featureChecker;
            _scoresheetRepository = scoresheetRepository;
            _scoresheetInstanceRepository = scoresheetInstanceRepository;
            _applicationFormRepository = applicationFormRepository;
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

            // Check if AI Reporting feature is enabled
            if (!await _featureChecker.IsEnabledAsync(FeatureConsts.AIReporting))
            {
                _logger.LogDebug("AI Reporting feature is disabled, skipping AI summary generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            // Check if AI service is available
            if (!await _aiService.IsAvailableAsync())
            {
                _logger.LogWarning("AI service is not available, skipping AI summary generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            _logger.LogInformation("Generating AI summaries for attachments in application {ApplicationId}.", eventData.Application.Id);

            try
            {
                // Get all CHEFS attachments for this application
                var attachments = await _attachmentRepository.GetListAsync(a => a.ApplicationId == eventData.Application.Id);

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
                                Guid.Parse(attachment.ChefsSumbissionId), 
                                Guid.Parse(attachment.ChefsFileId), 
                                attachment.FileName);

                            if (fileDto?.Content != null)
                            {
                                _logger.LogDebug("Processing {FileName} ({ContentType}, {Size} bytes) for AI summary generation", 
                                    attachment.FileName, fileDto.ContentType, fileDto.Content.Length);

                                // Generate AI summary with text extraction and file content analysis
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName, 
                                    fileDto.Content, 
                                    fileDto.ContentType);

                                // Update the attachment with the AI summary
                                attachment.AISummary = summary;
                                await _attachmentRepository.UpdateAsync(attachment);

                                _logger.LogDebug("Successfully generated AI summary for attachment {FileName}: {SummaryPreview}", 
                                    attachment.FileName, summary?.Substring(0, Math.Min(100, summary?.Length ?? 0)) + "...");
                            }
                            else
                            {
                                _logger.LogWarning("Could not retrieve content for attachment {FileName}", attachment.FileName);
                                
                                // Generate summary from filename only as fallback
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName, 
                                    new byte[0], 
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
                                attachment.FileName, 
                                new byte[0], 
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

                // After processing all attachments, perform application analysis
                await GenerateApplicationAnalysisAsync(eventData.Application, attachments);

                // Generate AI scoresheet answers
                await GenerateScoresheetAnalysisAsync(eventData.Application, attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summaries for application {ApplicationId}", eventData.Application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private async Task GenerateApplicationAnalysisAsync(Application application, List<ApplicationChefsFileAttachment> attachments)
        {
            try
            {
                _logger.LogDebug("Starting application analysis for {ApplicationId}", application.Id);

                // Skip if application already has analysis
                if (!string.IsNullOrEmpty(application.AIAnalysis))
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
                var applicationContent = $@"
Project Name: {application.ProjectName}
Reference Number: {application.ReferenceNo}
Requested Amount: ${application.RequestedAmount:N2}
Total Project Budget: ${application.TotalProjectBudget:N2}
Project Summary: {application.ProjectSummary ?? "Not provided"}
City: {application.City ?? "Not specified"}
Economic Region: {application.EconomicRegion ?? "Not specified"}
Community: {application.Community ?? "Not specified"}
Project Start Date: {application.ProjectStartDate?.ToShortDateString() ?? "Not specified"}
Project End Date: {application.ProjectEndDate?.ToShortDateString() ?? "Not specified"}
Submission Date: {application.SubmissionDate.ToShortDateString()}

FULL APPLICATION FORM SUBMISSION:
{formSubmission?.RenderedHTML ?? "Form submission content not available"}
";
                _logger.LogInformation("Generating analysis for following application:", applicationContent);

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

                // Update the application with the analysis
                application.AIAnalysis = analysis;
                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Successfully generated AI analysis: {AIAnalysis}", application.AIAnalysis);
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

                // Skip if application already has scoresheet analysis
                if (!string.IsNullOrEmpty(application.AIScoresheetAnswers))
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
                var applicationContent = $@"
Project Name: {application.ProjectName}
Reference Number: {application.ReferenceNo}
Requested Amount: ${application.RequestedAmount:N2}
Total Project Budget: ${application.TotalProjectBudget:N2}
Project Summary: {application.ProjectSummary ?? "Not provided"}
City: {application.City ?? "Not specified"}
Economic Region: {application.EconomicRegion ?? "Not specified"}
Community: {application.Community ?? "Not specified"}
Project Start Date: {application.ProjectStartDate?.ToShortDateString() ?? "Not specified"}
Project End Date: {application.ProjectEndDate?.ToShortDateString() ?? "Not specified"}
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

                        var sectionJson = JsonSerializer.Serialize(sectionQuestionsData, new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

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
                var combinedResults = JsonSerializer.Serialize(allSectionResults, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

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

                // Store in both locations: metadata for debugging and actual scoresheet answers for UI
                application.AIScoresheetAnswers = validatedJson;
                await _applicationRepository.UpdateAsync(application);

                // Create actual scoresheet answers if there are any
                _logger.LogError("About to check validatedJson: length={Length}, content={Content}", 
                    validatedJson?.Length ?? 0, validatedJson);
                
                if (!string.IsNullOrEmpty(validatedJson) && validatedJson != "{}")
                {
                    _logger.LogError("Calling CreateScoresheetAnswersFromAI for application {ApplicationId}", application.Id);
                    await CreateScoresheetAnswersFromAI(application, scoresheet, validatedJson, attachments);
                }
                else
                {
                    _logger.LogError("NOT calling CreateScoresheetAnswersFromAI - validatedJson is empty or null");
                }

                _logger.LogInformation("Successfully generated AI scoresheet answers for application {ApplicationId}", application.Id);
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
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                var startIndex = cleaned.IndexOf('\n');
                if (startIndex >= 0)
                {
                    cleaned = cleaned.Substring(startIndex + 1);
                }
            }
            else if (cleaned.StartsWith("```"))
            {
                // Handle generic ``` opening tag
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

        private static object? ExtractSelectListOptions(Unity.Flex.Domain.Scoresheets.Question field)
        {
            if (field.Type != Unity.Flex.Scoresheets.Enums.QuestionType.SelectList || string.IsNullOrEmpty(field.Definition))
                return null;

            try
            {
                var definition = JsonSerializer.Deserialize<Unity.Flex.Worksheets.Definitions.QuestionSelectListDefinition>(field.Definition);
                if (definition?.Options != null && definition.Options.Any())
                {
                    return definition.Options.Select((option, index) => new
                    {
                        number = index + 1,
                        value = option.Value,
                        numericValue = option.NumericValue
                    }).ToArray();
                }
            }
            catch (JsonException)
            {
                // If definition parsing fails, return null
            }

            return null;
        }

        private async Task CreateScoresheetAnswersFromAI(Unity.GrantManager.Applications.Application application, 
            Unity.Flex.Domain.Scoresheets.Scoresheet scoresheet, 
            string validatedJson, 
            List<ApplicationChefsFileAttachment> attachments)
        {
            _logger.LogError("CreateScoresheetAnswersFromAI: Method called for application {ApplicationId}", application.Id);
            
            try
            {
                // Find scoresheet instance for this application
                var scoresheetInstance = await _scoresheetInstanceRepository.GetByCorrelationAsync(application.Id);
                if (scoresheetInstance == null)
                {
                    _logger.LogError("No scoresheet instance found for application {ApplicationId}", application.Id);
                    return;
                }

                // Parse the AI answers
                using var aiDoc = JsonDocument.Parse(validatedJson);
                var answerCount = aiDoc.RootElement.EnumerateObject().Count();
                _logger.LogError("Found scoresheet instance, processing {AnswerCount} AI answers", answerCount);
                
                foreach (var aiAnswer in aiDoc.RootElement.EnumerateObject())
                {
                    try
                    {
                        var questionId = Guid.Parse(aiAnswer.Name);
                        
                        // Check if there's already a human answer for this question
                        var existingAnswer = scoresheetInstance.Answers.FirstOrDefault(a => a.QuestionId == questionId);
                        if (existingAnswer != null)
                        {
                            continue;
                        }

                        // Find the question to determine its type
                        var question = scoresheet.Sections
                            .SelectMany(s => s.Fields)
                            .FirstOrDefault(f => f.Id == questionId);
                            
                        if (question == null) continue;

                        // Extract the answer value
                        string answerValue;
                        if (aiAnswer.Value.ValueKind == JsonValueKind.Object && aiAnswer.Value.TryGetProperty("answer", out var answerProp))
                        {
                            answerValue = answerProp.ToString();
                        }
                        else
                        {
                            answerValue = aiAnswer.Value.ToString();
                        }

                        if (string.IsNullOrEmpty(answerValue)) continue;


                        // Create the proper JSON format for the answer based on question type
                        var currentValue = CreateAnswerValueJson(question.Type, answerValue);
                        
                        // Create the Answer record
                        var newAnswer = new Unity.Flex.Domain.Scoresheets.Answer(Guid.NewGuid())
                            .SetValue(currentValue);
                        newAnswer.QuestionId = questionId;
                        newAnswer.ScoresheetInstanceId = scoresheetInstance.Id;

                        // Add to the scoresheet instance
                        scoresheetInstance.Answers.Add(newAnswer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing AI answer for question {QuestionName}", aiAnswer.Name);
                        continue;
                    }
                }

                // Update the scoresheet instance
                await _scoresheetInstanceRepository.UpdateAsync(scoresheetInstance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scoresheet answers from AI for application {ApplicationId}", application.Id);
                // Don't throw - this is supplementary functionality
            }
        }

        private string CreateAnswerValueJson(Unity.Flex.Scoresheets.Enums.QuestionType questionType, string value)
        {
            object valueObject = questionType switch
            {
                Unity.Flex.Scoresheets.Enums.QuestionType.Text => new Unity.Flex.Worksheets.Values.TextValue(value),
                Unity.Flex.Scoresheets.Enums.QuestionType.Number => new Unity.Flex.Worksheets.Values.NumericValue(double.TryParse(value, out var num) ? num : 0),
                Unity.Flex.Scoresheets.Enums.QuestionType.YesNo => new Unity.Flex.Worksheets.Values.YesNoValue(value),
                Unity.Flex.Scoresheets.Enums.QuestionType.SelectList => new Unity.Flex.Worksheets.Values.SelectListValue(value),
                Unity.Flex.Scoresheets.Enums.QuestionType.TextArea => new Unity.Flex.Worksheets.Values.TextAreaValue(value),
                _ => new Unity.Flex.Worksheets.Values.TextValue(value)
            };

            return JsonSerializer.Serialize(valueObject);
        }


    }
}