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
using Volo.Abp.EventBus.Local;
using Unity.Flex.Domain.Scoresheets;
using System.Text.Json;
using Volo.Abp.Features;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class GenerateAIContentHandler : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        private const string ArrayFieldType = "array";
        private const string ObjectFieldType = "object";
        private const string AttachmentSummariesFeatureName = "Unity.AI.AttachmentSummaries";
        private const string ApplicationAnalysisFeatureName = "Unity.AI.ApplicationAnalysis";
        private const string ScoringFeatureName = "Unity.AI.Scoring";
        private readonly IAIService _aiService;
        private readonly IAttachmentAISummaryService _attachmentAISummaryService;
        private readonly IApplicationChefsFileAttachmentRepository _attachmentRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly ILogger<GenerateAIContentHandler> _logger;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
        private readonly IFeatureChecker _featureChecker;
        public ILocalEventBus LocalEventBus { get; set; } = NullLocalEventBus.Instance;
        private const string ComponentsKey = "components";
        private static readonly HashSet<string> NonDataComponentTypes = new()
        {
            "button", "simplebuttonadvanced", "html", "htmlelement", "content", "simpleseparator"
        };
        private static readonly string[] AllowedAnalysisRootProperties =
        {
            AIJsonKeys.Rating,
            AIJsonKeys.Errors,
            AIJsonKeys.Warnings,
            AIJsonKeys.Summaries,
            AIJsonKeys.NextSteps,
            AIJsonKeys.Recommendation
        };
        private static readonly string[] AllowedFindingProperties =
        {
            AIJsonKeys.Id,
            AIJsonKeys.Hidden,
            AIJsonKeys.Title,
            AIJsonKeys.Detail
        };
        private static readonly string[] AllowedRecommendationProperties =
        {
            AIJsonKeys.Decision,
            AIJsonKeys.Rationale
        };
        private static readonly string[] AllowedScoresheetAnswerProperties =
        {
            AIJsonKeys.Answer,
            AIJsonKeys.Rationale,
            AIJsonKeys.Confidence
        };

        private readonly JsonSerializerOptions _jsonOptionsIndented = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public GenerateAIContentHandler(
            IAIService aiService,
            IAttachmentAISummaryService attachmentAISummaryService,
            IApplicationChefsFileAttachmentRepository attachmentRepository,
            IApplicationRepository applicationRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ILogger<GenerateAIContentHandler> logger,
            IScoresheetRepository scoresheetRepository,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionRepository applicationFormVersionRepository,
            IFeatureChecker featureChecker)
        {
            _aiService = aiService;
            _attachmentAISummaryService = attachmentAISummaryService;
            _attachmentRepository = attachmentRepository;
            _applicationRepository = applicationRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _logger = logger;
            _scoresheetRepository = scoresheetRepository;
            _applicationFormRepository = applicationFormRepository;
            _applicationFormVersionRepository = applicationFormVersionRepository;
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
                _logger.LogWarning("Event data or application is null in GenerateAIContentHandler.");
                return;
            }

            // Check if either AI feature is enabled
            var attachmentSummariesEnabled = await _featureChecker.IsEnabledAsync(AttachmentSummariesFeatureName);
            var applicationAnalysisEnabled = await _featureChecker.IsEnabledAsync(ApplicationAnalysisFeatureName);
            var scoringEnabled = await _featureChecker.IsEnabledAsync(ScoringFeatureName);

            if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
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
                        await ProcessAttachmentSummaryAsync(attachment, eventData.Application.Id);
                    }
                }

                // Generate application analysis if feature is enabled
                if (applicationAnalysisEnabled)
                {
                    await GenerateApplicationAnalysisAsync(eventData.Application, attachments);
                }

                // Generate AI scoresheet answers if feature is enabled
                if (scoringEnabled)
                {
                    await GenerateScoresheetAnalysisAsync(eventData.Application, attachments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI content for application {ApplicationId}", eventData.Application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private async Task ProcessAttachmentSummaryAsync(ApplicationChefsFileAttachment attachment, Guid applicationId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(attachment.AISummary))
                {
                    _logger.LogDebug("Skipping AI summary for attachment {FileName} - already has summary", attachment.FileName);
                    return;
                }

                _logger.LogDebug("Generating AI summary for attachment {FileName}", attachment.FileName);
                await _attachmentAISummaryService.GenerateAndSaveAsync(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary for attachment {FileName} in application {ApplicationId}",
                    attachment.FileName, applicationId);
                // Continue processing other attachments even if one fails
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

                var analysisAttachments = BuildAnalysisAttachments(attachments);

                // Get form submission content including rendered HTML
                var formSubmission = await _applicationFormSubmissionRepository
                    .GetByApplicationAsync(application.Id);

                var formFieldSchema = BuildEmptyFormFieldSchema();

                if (formSubmission?.ApplicationFormVersionId is Guid formVersionId)
                {
                    formFieldSchema = await ExtractFormFieldConfigurationSchemaAsync(formVersionId);
                    _logger.LogDebug("Extracted form field schema for application {ApplicationId}",
                        application.Id);
                }
                else
                {
                    _logger.LogWarning("Could not extract form field schema for application {ApplicationId} - ApplicationFormVersionId is null",
                        application.Id);
                }

                var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);
                var analysisData = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, _logger);
                _logger.LogInformation("Generating analysis for application {ApplicationId}", application.Id);

                _logger.LogDebug("Generating AI analysis for application {ApplicationId} with {AttachmentCount} attachment summaries",
                    application.Id, analysisAttachments.Count);

                var analysisRequest = new ApplicationAnalysisRequest
                {
                    Schema = formFieldSchema,
                    Data = analysisData,
                    Attachments = analysisAttachments
                };

                var analysis = await _aiService.GenerateApplicationAnalysisAsync(analysisRequest);
                var analysisJson = JsonSerializer.Serialize(analysis, _jsonOptionsIndented);
                if (!IsValidAnalysisPayload(analysisJson))
                {
                    _logger.LogWarning("Skipping invalid AI analysis payload for application {ApplicationId}.", application.Id);
                    return;
                }

                // Update the tracked application with the analysis
                trackedApplication.AIAnalysis = analysisJson;
                await _applicationRepository.UpdateAsync(trackedApplication);

                _logger.LogInformation("Successfully generated AI analysis for application {ApplicationId}", application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application analysis for {ApplicationId}", application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private static List<AIAttachmentItem> BuildAnalysisAttachments(List<ApplicationChefsFileAttachment> attachments)
        {
            return attachments
                .Where(a => !string.IsNullOrWhiteSpace(a.AISummary))
                .Select(a => new AIAttachmentItem
                {
                    Name = string.IsNullOrWhiteSpace(a.FileName) ? "attachment" : a.FileName.Trim(),
                    Summary = a.AISummary!.Trim()
                })
                .ToList();
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

                var allSectionResults = new Dictionary<string, object>();
                var scoresheetAttachments = BuildScoresheetAttachments(attachments);
                var formSubmission = await _applicationFormSubmissionRepository.GetByApplicationAsync(application.Id);
                var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);
                var scoresheetData = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, _logger);
                LogFormSubmissionPreview(formSubmission?.RenderedHTML);

                foreach (var section in scoresheet.Sections.OrderBy(s => s.Order))
                {
                    var sectionSchema = BuildScoresheetSectionSchema(section.Fields);
                    await ProcessScoresheetSectionAsync(
                        section.Name,
                        section.Fields.Count,
                        sectionSchema,
                        application.Id,
                        scoresheetData,
                        scoresheetAttachments,
                        allSectionResults);
                }

                await SaveScoresheetResultsAsync(trackedApplication, allSectionResults);
                await LocalEventBus.PublishAsync(new AiScoresheetAnswersGeneratedEvent
                {
                    Application = application
                });

                _logger.LogInformation("Successfully generated and saved AI scoresheet answers for application {ApplicationId}. Answers will be parsed when scoresheet instance is created.", application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet analysis for {ApplicationId}", application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private static List<AIAttachmentItem> BuildScoresheetAttachments(List<ApplicationChefsFileAttachment> attachments)
        {
            return attachments
                .Where(a => !string.IsNullOrEmpty(a.AISummary))
                .Select(a => new AIAttachmentItem
                {
                    Name = string.IsNullOrWhiteSpace(a.FileName) ? "attachment" : a.FileName.Trim(),
                    Summary = a.AISummary!.Trim()
                })
                .ToList();
        }

        private void LogFormSubmissionPreview(string? renderedFormHtml)
        {
            _logger.LogInformation("Form submission HTML length: {HtmlLength} characters", renderedFormHtml?.Length ?? 0);
            if (renderedFormHtml?.Length > 100)
            {
                _logger.LogDebug("Form submission HTML is present and non-trivial.");
            }
            else
            {
                _logger.LogWarning("Form submission HTML is missing or very short.");
            }
        }

        private async Task ProcessScoresheetSectionAsync(
            string sectionName,
            int questionCount,
            JsonElement sectionSchema,
            Guid applicationId,
            JsonElement scoresheetData,
            List<AIAttachmentItem> scoresheetAttachments,
            Dictionary<string, object> allSectionResults)
        {
            try
            {
                _logger.LogDebug("Processing section {SectionName} for application {ApplicationId}",
                    sectionName, applicationId);
                var sectionAnswers = await _aiService.GenerateScoresheetSectionAsync(new ScoresheetSectionRequest
                {
                    Data = scoresheetData,
                    Attachments = scoresheetAttachments,
                    SectionName = sectionName,
                    SectionSchema = sectionSchema
                });

                if (sectionAnswers.Answers.Count == 0)
                {
                    return;
                }

                var expectedQuestionIds = ExtractSectionQuestionIds(sectionSchema);
                var returnedQuestionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var answerEntry in sectionAnswers.Answers)
                {
                    returnedQuestionIds.Add(answerEntry.Key);
                    allSectionResults[answerEntry.Key] = new Dictionary<string, object?>
                    {
                        [AIJsonKeys.Answer] = answerEntry.Value.Answer,
                        [AIJsonKeys.Rationale] = answerEntry.Value.Rationale,
                        [AIJsonKeys.Confidence] = answerEntry.Value.Confidence
                    };
                }

                var missingQuestionIds = expectedQuestionIds.Except(returnedQuestionIds, StringComparer.OrdinalIgnoreCase).ToArray();
                if (missingQuestionIds.Length > 0)
                {
                    _logger.LogWarning(
                        "AI scoresheet response missing question answers for section {SectionName} in application {ApplicationId}. Expected: {ExpectedCount}, Returned: {ReturnedCount}, MissingIds: {MissingIds}.",
                        sectionName,
                        applicationId,
                        expectedQuestionIds.Count,
                        returnedQuestionIds.Count,
                        string.Join(",", missingQuestionIds));
                }
                else
                {
                    _logger.LogDebug(
                        "AI scoresheet response complete for section {SectionName} in application {ApplicationId}. Returned {ReturnedCount} answers.",
                        sectionName,
                        applicationId,
                        returnedQuestionIds.Count);
                }

                _logger.LogDebug("Successfully processed section {SectionName} with {QuestionCount} questions",
                    sectionName, questionCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing section {SectionName} for application {ApplicationId}",
                    sectionName, applicationId);
            }
        }

        private static HashSet<string> ExtractSectionQuestionIds(JsonElement sectionSchema)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (sectionSchema.ValueKind != JsonValueKind.Array)
            {
                return ids;
            }

            foreach (var question in sectionSchema.EnumerateArray())
            {
                if (question.ValueKind == JsonValueKind.Object &&
                    question.TryGetProperty("id", out var idProp) &&
                    idProp.ValueKind == JsonValueKind.String)
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

        private static JsonElement BuildScoresheetSectionSchema(IEnumerable<Unity.Flex.Domain.Scoresheets.Question> fields)
        {
            var sectionQuestionsData = fields
                .OrderBy(f => f.Order)
                .Select(field =>
                {
                    var options = ExtractSelectListOptions(field);
                    return new
                    {
                        id = field.Id.ToString(),
                        question = field.Label,
                        description = field.Description,
                        type = field.Type.ToString(),
                        options,
                        allowed_answers = ExtractSelectListOptionNumbers(options)
                    };
                })
                .ToList();

            return JsonSerializer.SerializeToElement(sectionQuestionsData);
        }

        private async Task SaveScoresheetResultsAsync(Application trackedApplication, Dictionary<string, object> allSectionResults)
        {
            var combinedResults = JsonSerializer.Serialize(allSectionResults, _jsonOptionsIndented);
            if (!IsValidScoresheetAnswersPayload(combinedResults))
            {
                _logger.LogWarning("Skipping invalid AI scoresheet payload for application {ApplicationId}.", trackedApplication.Id);
                return;
            }

            trackedApplication.AIScoresheetAnswers = combinedResults;
            await _applicationRepository.UpdateAsync(trackedApplication);
        }

        private static object[]? ExtractSelectListOptions(Unity.Flex.Domain.Scoresheets.Question field)
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
                // If definition parsing fails, return null
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

        /// <summary>
        /// Extracts form field metadata keyed by field key for analysis schema prompts.
        /// </summary>
        private async Task<JsonElement> ExtractFormFieldConfigurationSchemaAsync(Guid formVersionId)
        {
            try
            {
                var formVersion = await _applicationFormVersionRepository.GetAsync(formVersionId);
                if (formVersion == null || string.IsNullOrEmpty(formVersion.FormSchema))
                {
                    return BuildEmptyFormFieldSchema();
                }

                var schema = JObject.Parse(formVersion.FormSchema);
                var components = schema[ComponentsKey] as JArray;

                if (components == null || components.Count == 0)
                {
                    return BuildEmptyFormFieldSchema();
                }

                var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ExtractFieldRequirements(components, fields, string.Empty);
                return JsonSerializer.SerializeToElement(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting form field schema for form version {FormVersionId}", formVersionId);
                return BuildEmptyFormFieldSchema();
            }
        }

        private async Task<string?> GetFormSchemaAsync(Guid? formVersionId)
        {
            if (formVersionId == null)
            {
                return null;
            }

            try
            {
                var formVersion = await _applicationFormVersionRepository.GetAsync(formVersionId.Value);
                return string.IsNullOrWhiteSpace(formVersion?.FormSchema) ? null : formVersion.FormSchema;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load form schema for prompt data generation for form version {FormVersionId}.", formVersionId);
                return null;
            }
        }

        private static JsonElement BuildEmptyFormFieldSchema()
        {
            return JsonSerializer.SerializeToElement(new Dictionary<string, string>());
        }

        /// <summary>
        /// Recursively extracts form field metadata from form components
        /// </summary>
        private static void ExtractFieldRequirements(
            JArray components,
            Dictionary<string, string> fields,
            string currentPath)
        {
            foreach (var component in components.OfType<JObject>())
            {
                var key = component["key"]?.ToString();
                var label = component["label"]?.ToString();
                var type = component["type"]?.ToString();

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(type) || NonDataComponentTypes.Contains(type))
                {
                    ProcessNestedFieldRequirements(component, type, fields, currentPath);
                    continue;
                }

                var displayName = !string.IsNullOrEmpty(label) ? $"{label} ({key})" : key;
                var fullPath = string.IsNullOrEmpty(currentPath) ? displayName : $"{currentPath} > {displayName}";

                if (component["input"]?.Value<bool>() == true)
                {
                    fields[key] = NormalizeFieldType(type, component);
                }

                ProcessNestedFieldRequirements(component, type, fields, fullPath);
            }
        }

        /// <summary>
        /// Processes nested components for different container types
        /// </summary>
        private static void ProcessNestedFieldRequirements(
            JObject component,
            string? type,
            Dictionary<string, string> fields,
            string currentPath)
        {
            switch (type)
            {
                case "panel":
                case "simplepanel":
                case "fieldset":
                case "well":
                case "container":
                case "datagrid":
                case "table":
                    var nestedComponents = component[ComponentsKey] as JArray;
                    if (nestedComponents != null)
                    {
                        ExtractFieldRequirements(nestedComponents, fields, currentPath);
                    }
                    break;

                case "columns":
                case "simplecols2":
                case "simplecols3":
                case "simplecols4":
                    var columns = component["columns"] as JArray;
                    if (columns != null)
                    {
                        foreach (var column in columns.OfType<JObject>())
                        {
                            var columnComponents = column[ComponentsKey] as JArray;
                            if (columnComponents != null)
                            {
                                ExtractFieldRequirements(columnComponents, fields, currentPath);
                            }
                        }
                    }
                    break;

                case "tabs":
                case "simpletabs":
                    var tabs = component[ComponentsKey] as JArray;
                    if (tabs != null)
                    {
                        foreach (var tab in tabs.OfType<JObject>())
                        {
                            var tabComponents = tab[ComponentsKey] as JArray;
                            if (tabComponents != null)
                            {
                                ExtractFieldRequirements(tabComponents, fields, currentPath);
                            }
                        }
                    }
                    break;
            }
        }

        private static string NormalizeFieldType(string rawType, JObject component)
        {
            if (component["multiple"]?.Value<bool>() == true)
            {
                return ArrayFieldType;
            }

            return rawType.ToLowerInvariant() switch
            {
                "number" => "number",
                "currency" => "number",
                "checkbox" => "boolean",
                "datetime" => "date",
                "day" => "date",
                "date" => "date",
                "time" => "date",
                "datagrid" => ArrayFieldType,
                "editgrid" => ArrayFieldType,
                "table" => ArrayFieldType,
                "container" => ObjectFieldType,
                "panel" => ObjectFieldType,
                "fieldset" => ObjectFieldType,
                "well" => ObjectFieldType,
                _ => "string"
            };
        }

        private static bool IsValidAnalysisPayload(string analysisJson)
        {
            if (string.IsNullOrWhiteSpace(analysisJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(analysisJson);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (!root.TryGetProperty(AIJsonKeys.Rating, out var overallScore) ||
                    overallScore.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                if (!root.TryGetProperty(AIJsonKeys.Errors, out var errors) ||
                    !root.TryGetProperty(AIJsonKeys.Warnings, out var warnings) ||
                    !root.TryGetProperty(AIJsonKeys.Summaries, out var summaries) ||
                    !root.TryGetProperty(AIJsonKeys.NextSteps, out var nextSteps) ||
                    !root.TryGetProperty(AIJsonKeys.Recommendation, out var recommendation))
                {
                    return false;
                }

                if (!HasOnlyAllowedProperties(root, AllowedAnalysisRootProperties))
                {
                    return false;
                }

                if (!IsValidFindingsArray(errors) ||
                    !IsValidFindingsArray(warnings) ||
                    !IsValidFindingsArray(summaries) ||
                    !IsValidFindingsArray(nextSteps) ||
                    !IsValidRecommendation(recommendation))
                {
                    return false;
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IsValidFindingsArray(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var finding in element.EnumerateArray())
            {
                if (finding.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (!finding.TryGetProperty(AIJsonKeys.Title, out var title) || title.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                if (!finding.TryGetProperty(AIJsonKeys.Detail, out var detail) || detail.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                if (!finding.TryGetProperty(AIJsonKeys.Hidden, out var hidden) ||
                    (hidden.ValueKind != JsonValueKind.True && hidden.ValueKind != JsonValueKind.False))
                {
                    return false;
                }

                if (!HasOnlyAllowedProperties(finding, AllowedFindingProperties))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidRecommendation(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!element.TryGetProperty(AIJsonKeys.Decision, out var decision) || decision.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var decisionValue = decision.GetString();
            if (!string.Equals(decisionValue, "PROCEED", StringComparison.Ordinal) &&
                !string.Equals(decisionValue, "HOLD", StringComparison.Ordinal))
            {
                return false;
            }

            if (!element.TryGetProperty(AIJsonKeys.Rationale, out var rationale) || rationale.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return HasOnlyAllowedProperties(element, AllowedRecommendationProperties);
        }

        private static bool IsValidScoresheetAnswersPayload(string scoresheetJson)
        {
            if (string.IsNullOrWhiteSpace(scoresheetJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(scoresheetJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                foreach (var question in doc.RootElement.EnumerateObject().Select(question => question.Value))
                {
                    if (question.ValueKind != JsonValueKind.Object)
                    {
                        return false;
                    }

                    if (!question.TryGetProperty(AIJsonKeys.Answer, out var answer))
                    {
                        return false;
                    }

                    var validAnswerType = answer.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False;
                    if (!validAnswerType)
                    {
                        return false;
                    }

                    if (!question.TryGetProperty(AIJsonKeys.Rationale, out var rationale) || rationale.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    if (!question.TryGetProperty(AIJsonKeys.Confidence, out var confidence) || !confidence.TryGetInt32(out var confidenceValue))
                    {
                        return false;
                    }

                    if (confidenceValue < 0 || confidenceValue > 100)
                    {
                        return false;
                    }

                    if (!HasOnlyAllowedProperties(question, AllowedScoresheetAnswerProperties))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasOnlyAllowedProperties(JsonElement element, IReadOnlyCollection<string> allowedProperties)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var allowed = new HashSet<string>(allowedProperties, StringComparer.Ordinal);
            return element.EnumerateObject().All(property => allowed.Contains(property.Name));
        }

    }
}
