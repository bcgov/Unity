using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class OpenAIService : IAIService, ITransientDependency
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly ITextExtractionService _textExtractionService;
        private const string ApplicationAnalysisPromptType = "ApplicationAnalysis";
        private const string AttachmentSummaryPromptType = "AttachmentSummary";
        private const string ScoresheetSectionPromptType = "ScoresheetSection";
        private const string PromptVersionV0 = "v0";
        private const string PromptVersionV1 = "v1";
        private const string PromptTemplatesFolder = "AI\\Prompts\\Versions";
        private const string AnalysisSystemTemplateName = "analysis.system";
        private const string AnalysisUserTemplateName = "analysis.user";
        private const string AnalysisRubricTemplateName = "analysis.rubric";
        private const string AttachmentSystemTemplateName = "attachment.system";
        private const string AttachmentUserTemplateName = "attachment.user";
        private const string ScoresheetSystemTemplateName = "scoresheet.system";
        private const string ScoresheetUserTemplateName = "scoresheet.user";
        private const string ScoresheetOutputTemplateName = "scoresheet.output";
        private const string DefaultScoresheetOutputTemplate = @"{
  ""<question_id>"": {
    ""answer"": ""<string | number>"",
    ""rationale"": ""<evidence-based rationale>"",
    ""confidence"": <integer 0-100 step 5>
  }
}";
        private const string NoSummaryGeneratedMessage = "No summary generated.";
        private const string ServiceNotConfiguredMessage = "AI analysis not available - service not configured.";
        private const string ServiceTemporarilyUnavailableMessage = "AI analysis failed - service temporarily unavailable.";
        private const string SummaryFailedRetryMessage = "AI analysis failed - please try again later.";

        private string? ApiKey => _configuration["Azure:OpenAI:ApiKey"];
        private string? ApiUrl => _configuration["Azure:OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
        private readonly string MissingApiKeyMessage = "OpenAI API key is not configured";

        // Optional local debugging sink for prompt payload logs to a local file.
        // Not intended for deployed/shared environments.
        private bool IsPromptFileLoggingEnabled => _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
        private const string PromptLogDirectoryName = "logs";
        private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

        private static readonly JsonSerializerOptions JsonLogOptions = new() { WriteIndented = true };

        private static readonly IReadOnlyDictionary<string, string> PromptProfiles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PromptVersionV0] = PromptVersionV0,
                [PromptVersionV1] = PromptVersionV1
            };
        private static readonly Dictionary<string, string> PromptTemplateCache = new(StringComparer.OrdinalIgnoreCase);

        private string SelectedPromptVersion => ResolvePromptVersion(_configuration["Azure:OpenAI:PromptVersion"]);

        public OpenAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIService> logger,
            ITextExtractionService textExtractionService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _textExtractionService = textExtractionService;
        }

        public Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public async Task<AICompletionResponse> GenerateCompletionAsync(AICompletionRequest request)
        {
            var content = await GenerateSummaryAsync(
                request?.UserPrompt ?? string.Empty,
                request?.SystemPrompt,
                request?.MaxTokens ?? 150);
            return new AICompletionResponse { Content = content };
        }

        public async Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request)
        {
            var data = JsonSerializer.Serialize(request.Data, JsonLogOptions);
            var schema = JsonSerializer.Serialize(request.Schema, JsonLogOptions);

            var attachmentsPayload = request.Attachments
                .Select(a => new
                {
                    name = string.IsNullOrWhiteSpace(a.Name) ? "attachment" : a.Name.Trim(),
                    summary = string.IsNullOrWhiteSpace(a.Summary) ? string.Empty : a.Summary.Trim()
                })
                .Cast<object>();

            var attachments = JsonSerializer.Serialize(attachmentsPayload, JsonLogOptions);
            var systemPrompt = BuildAnalysisSystemPrompt(SelectedPromptVersion);
            var analysisContent = BuildAnalysisUserPrompt(
                SelectedPromptVersion,
                schema,
                data,
                attachments,
                request.Rubric);
            await LogPromptInputAsync(ApplicationAnalysisPromptType, systemPrompt, analysisContent);
            var raw = await GenerateSummaryAsync(analysisContent, systemPrompt, 1000);
            await LogPromptOutputAsync(ApplicationAnalysisPromptType, raw);
            return ParseApplicationAnalysisResponse(AddIdsToAnalysisItems(raw));
        }

        public async Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return ServiceNotConfiguredMessage;
            }

            _logger.LogDebug("Calling OpenAI chat completions. PromptLength: {PromptLength}, MaxTokens: {MaxTokens}", content?.Length ?? 0, maxTokens);

            try
            {
                var systemPrompt = prompt ?? "You are a professional grant analyst for the BC Government.";
                var userPrompt = content ?? string.Empty;

                var requestBody = new
                {
                    messages = new[]
                    {
                       new { role = "system", content = systemPrompt },
                       new { role = "user", content = userPrompt }
                   },
                    max_tokens = maxTokens,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);

                var response = await _httpClient.PostAsync(ApiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug(
                    "OpenAI chat completions response received. StatusCode: {StatusCode}, ResponseLength: {ResponseLength}",
                    response.StatusCode,
                    responseContent?.Length ?? 0);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return ServiceTemporarilyUnavailableMessage;
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return NoSummaryGeneratedMessage;
                }

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var choices = jsonDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    return message.GetProperty("content").GetString() ?? NoSummaryGeneratedMessage;
                }

                return NoSummaryGeneratedMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary");
                return SummaryFailedRetryMessage;
            }
        }

        public async Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType)
        {
            try
            {
                var extractedText = await _textExtractionService.ExtractTextAsync(fileName, fileContent, contentType);

                var prompt = BuildAttachmentSystemPrompt(SelectedPromptVersion);

                var attachmentText = string.IsNullOrWhiteSpace(extractedText) ? null : extractedText;
                if (attachmentText != null)
                {
                    _logger.LogDebug("Extracted {TextLength} characters from {FileName}", extractedText.Length, fileName);
                }
                else
                {
                    _logger.LogDebug("No text extracted from {FileName}, analyzing metadata only", fileName);
                }

                var attachmentPayload = new
                {
                    name = fileName,
                    contentType,
                    sizeBytes = fileContent.Length,
                    text = attachmentText
                };
                var attachment = JsonSerializer.Serialize(attachmentPayload, JsonLogOptions);
                var contentToAnalyze = BuildAttachmentUserPrompt(SelectedPromptVersion, attachment);

                await LogPromptInputAsync(AttachmentSummaryPromptType, prompt, contentToAnalyze);
                var modelOutput = await GenerateSummaryAsync(contentToAnalyze, prompt, 150);
                await LogPromptOutputAsync(AttachmentSummaryPromptType, modelOutput);
                return ExtractSummaryFromJson(modelOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attachment summary for {FileName}", fileName);
                return $"AI analysis not available for this attachment ({fileName}).";
            }
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request)
        {
            var summary = await GenerateAttachmentSummaryAsync(
                request?.FileName ?? string.Empty,
                request?.FileContent ?? Array.Empty<byte>(),
                request?.ContentType ?? "application/octet-stream");
            return new AttachmentSummaryResponse { Summary = summary };
        }

        public async Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric, string? formFieldConfiguration = null)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage);
                return ServiceNotConfiguredMessage;
            }

            try
            {
                object schemaPayload = new { };
                if (!string.IsNullOrWhiteSpace(formFieldConfiguration))
                {
                    try
                    {
                        using var schemaDoc = JsonDocument.Parse(formFieldConfiguration);
                        schemaPayload = schemaDoc.RootElement.Clone();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Invalid form field configuration JSON. Using empty schema payload.");
                    }
                }

                var dataPayload = new
                {
                    applicationContent
                };

                var attachmentsPayload = attachmentSummaries?.Count > 0
                    ? attachmentSummaries
                        .Select((summary, index) => new
                        {
                            name = $"Attachment {index + 1}",
                            summary = summary
                        })
                        .Cast<object>()
                    : Enumerable.Empty<object>();

                var schema = JsonSerializer.Serialize(schemaPayload, JsonLogOptions);
                var data = JsonSerializer.Serialize(dataPayload, JsonLogOptions);
                var attachments = JsonSerializer.Serialize(attachmentsPayload, JsonLogOptions);
                var analysisContent = BuildAnalysisUserPrompt(
                    SelectedPromptVersion,
                    schema,
                    data,
                    attachments,
                    rubric);
                var systemPrompt = BuildAnalysisSystemPrompt(SelectedPromptVersion);

                await LogPromptInputAsync(ApplicationAnalysisPromptType, systemPrompt, analysisContent);
                var rawAnalysis = await GenerateSummaryAsync(analysisContent, systemPrompt, 1000);
                await LogPromptOutputAsync(ApplicationAnalysisPromptType, rawAnalysis);

                // Post-process the AI response to add unique IDs to errors and warnings
                return AddIdsToAnalysisItems(rawAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing application");
                return SummaryFailedRetryMessage;
            }
        }

        private string AddIdsToAnalysisItems(string analysisJson)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(analysisJson);
                using var memoryStream = new System.IO.MemoryStream();
                using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    foreach (var property in jsonDoc.RootElement.EnumerateObject())
                    {
                        var outputPropertyName = property.Name;

                        if (outputPropertyName == AIJsonKeys.Errors || outputPropertyName == AIJsonKeys.Warnings)
                        {
                            writer.WritePropertyName(outputPropertyName);
                            writer.WriteStartArray();

                            foreach (var item in property.Value.EnumerateArray())
                            {
                                writer.WriteStartObject();

                                // Add unique ID first
                                writer.WriteString("id", Guid.NewGuid().ToString());

                                // Copy existing properties
                                foreach (var itemProperty in item.EnumerateObject())
                                {
                                    itemProperty.WriteTo(writer);
                                }

                                writer.WriteEndObject();
                            }

                            writer.WriteEndArray();
                        }
                        else
                        {
                            if (outputPropertyName != property.Name)
                            {
                                writer.WritePropertyName(outputPropertyName);
                                property.Value.WriteTo(writer);
                                continue;
                            }

                            property.WriteTo(writer);
                        }
                    }

                    // Add dismissed array if not present.
                    if (!jsonDoc.RootElement.TryGetProperty(AIJsonKeys.Dismissed, out _))
                    {
                        writer.WritePropertyName(AIJsonKeys.Dismissed);
                        writer.WriteStartArray();
                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding IDs to analysis items, returning original JSON");
                return analysisJson; // Return original if processing fails
            }
        }

        public async Task<string> GenerateScoresheetSectionAnswersAsync(string applicationContent, List<string> attachmentSummaries, string sectionJson, string sectionName)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage);
                return "{}";
            }

            try
            {
                var attachments = attachmentSummaries?.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                object sectionQuestionsPayload = sectionJson;
                if (!string.IsNullOrWhiteSpace(sectionJson))
                {
                    try
                    {
                        using var sectionDoc = JsonDocument.Parse(sectionJson);
                        sectionQuestionsPayload = sectionDoc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        // Keep raw string payload when JSON parsing fails.
                    }
                }

                var sectionPayload = new
                {
                    name = sectionName,
                    questions = sectionQuestionsPayload
                };
                var section = JsonSerializer.Serialize(sectionPayload, JsonLogOptions);
                var response = BuildScoresheetSectionResponseTemplate(SelectedPromptVersion, section);

                var analysisContent = BuildScoresheetSectionUserPrompt(
                    SelectedPromptVersion,
                    applicationContent,
                    attachments,
                    section,
                    response);

                var systemPrompt = BuildScoresheetSectionSystemPrompt(SelectedPromptVersion);

                await LogPromptInputAsync(ScoresheetSectionPromptType, systemPrompt, analysisContent);
                var modelOutput = await GenerateSummaryAsync(analysisContent, systemPrompt, 2000);
                await LogPromptOutputAsync(ScoresheetSectionPromptType, modelOutput);
                return modelOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet section answers for section {SectionName}", sectionName);
                return "{}";
            }
        }

        public async Task<ScoresheetSectionResponse> GenerateScoresheetSectionAnswersAsync(ScoresheetSectionRequest request)
        {
            var dataJson = JsonSerializer.Serialize(request.Data, JsonLogOptions);
            var sectionJson = JsonSerializer.Serialize(request.SectionSchema, JsonLogOptions);

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();

            var raw = await GenerateScoresheetSectionAnswersAsync(
                dataJson,
                attachmentSummaries,
                sectionJson,
                request.SectionName);
            return ParseScoresheetSectionResponse(raw);
        }

        private static ApplicationAnalysisResponse ParseApplicationAnalysisResponse(string raw)
        {
            var response = new ApplicationAnalysisResponse();

            if (!TryParseJsonObjectFromResponse(raw, out var root))
            {
                return response;
            }

            if (TryGetStringProperty(root, AIJsonKeys.Rating, out var rating))
            {
                response.Rating = rating;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                response.Errors = ParseFindings(errors);
            }

            if (root.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
            {
                response.Warnings = ParseFindings(warnings);
            }

            if (root.TryGetProperty(AIJsonKeys.Summaries, out var summaries) && summaries.ValueKind == JsonValueKind.Array)
            {
                response.Summaries = ParseFindings(summaries);
            }

            if (root.TryGetProperty(AIJsonKeys.Dismissed, out var dismissed) && dismissed.ValueKind == JsonValueKind.Array)
            {
                response.Dismissed = dismissed
                    .EnumerateArray()
                    .Select(GetStringValueOrNull)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Cast<string>()
                    .ToList();
            }

            return response;
        }

        private static bool TryGetStringProperty(JsonElement root, string propertyName, out string? value)
        {
            value = null;
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string? GetStringValueOrNull(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            return null;
        }

        private static List<ApplicationAnalysisFinding> ParseFindings(JsonElement array)
        {
            var findings = new List<ApplicationAnalysisFinding>();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var id = item.TryGetProperty(AIJsonKeys.Id, out var idProp) && idProp.ValueKind == JsonValueKind.String
                    ? idProp.GetString()
                    : null;
                string? title = null;
                if (item.TryGetProperty(AIJsonKeys.Title, out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    title = titleProp.GetString();
                }

                string? detail = null;
                if (item.TryGetProperty(AIJsonKeys.Detail, out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    detail = detailProp.GetString();
                }

                findings.Add(new ApplicationAnalysisFinding
                {
                    Id = id,
                    Title = title,
                    Detail = detail
                });
            }

            return findings;
        }

        private static ScoresheetSectionResponse ParseScoresheetSectionResponse(string raw)
        {
            var response = new ScoresheetSectionResponse();
            if (!TryParseJsonObjectFromResponse(raw, out var root))
            {
                return response;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var answer = property.Value.TryGetProperty("answer", out var answerProp)
                    ? answerProp.Clone()
                    : default;
                var rationale = property.Value.TryGetProperty("rationale", out var rationaleProp) &&
                                rationaleProp.ValueKind == JsonValueKind.String
                    ? rationaleProp.GetString() ?? string.Empty
                    : string.Empty;
                var confidence = property.Value.TryGetProperty("confidence", out var confidenceProp) &&
                                 confidenceProp.ValueKind == JsonValueKind.Number &&
                                 confidenceProp.TryGetInt32(out var parsedConfidence)
                    ? NormalizeConfidence(parsedConfidence)
                    : 0;

                response.Answers[property.Name] = new ScoresheetSectionAnswer
                {
                    Answer = answer,
                    Rationale = rationale,
                    Confidence = confidence
                };
            }

            return response;
        }

        private static int NormalizeConfidence(int confidence)
        {
            var clamped = Math.Clamp(confidence, 0, 100);
            var rounded = (int)Math.Round(clamped / 5.0, MidpointRounding.AwayFromZero) * 5;
            return Math.Clamp(rounded, 0, 100);
        }

        private static string BuildScoresheetSectionResponseTemplate(string version, string sectionPayloadJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(sectionPayloadJson);
                if (!doc.RootElement.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
                {
                    return BuildScoresheetSectionOutputTemplate(version);
                }

                var template = new Dictionary<string, object>();
                foreach (var question in questions.EnumerateArray())
                {
                    if (!question.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var questionId = idProp.GetString();
                    if (string.IsNullOrWhiteSpace(questionId))
                    {
                        continue;
                    }

                    template[questionId] = new
                    {
                        answer = string.Empty,
                        rationale = string.Empty,
                        confidence = 0
                    };
                }

                if (template.Count == 0)
                {
                    return BuildScoresheetSectionOutputTemplate(version);
                }

                return JsonSerializer.Serialize(template, JsonLogOptions);
            }
            catch (JsonException)
            {
                return BuildScoresheetSectionOutputTemplate(version);
            }
        }

        private static string BuildScoresheetSectionOutputTemplate(string version)
        {
            if (TryGetPromptTemplate(version, ScoresheetOutputTemplateName, out var template))
            {
                return template;
            }

            if (string.Equals(version, PromptVersionV0, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultScoresheetOutputTemplate;
            }

            throw new InvalidOperationException(
                $"Missing required prompt template '{ScoresheetOutputTemplateName}.txt' for prompt version '{version}'.");
        }

        private async Task LogPromptInputAsync(string promptType, string? systemPrompt, string userPrompt)
        {
            var formattedInput = FormatPromptInputForLog(systemPrompt, userPrompt);
            _logger.LogInformation("AI {PromptType} ({PromptVersion}) input payload: {PromptInput}", promptType, SelectedPromptVersion, formattedInput);
            await WritePromptLogFileAsync(promptType, "INPUT", formattedInput);
        }

        private async Task LogPromptOutputAsync(string promptType, string output)
        {
            var formattedOutput = FormatPromptOutputForLog(output);
            _logger.LogInformation("AI {PromptType} ({PromptVersion}) model output payload: {ModelOutput}", promptType, SelectedPromptVersion, formattedOutput);
            await WritePromptLogFileAsync(promptType, "OUTPUT", formattedOutput);
        }

        private async Task WritePromptLogFileAsync(string promptType, string payloadType, string payload)
        {
            if (!CanWritePromptFileLog())
            {
                return;
            }

            try
            {
                var now = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
                var logDirectory = Path.Combine(AppContext.BaseDirectory, PromptLogDirectoryName);
                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, PromptLogFileName);
                var entry = $"{now} [{promptType}] {payloadType}\n{payload}\n\n";
                await File.AppendAllTextAsync(logPath, entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write AI prompt log file.");
            }
        }

        private bool CanWritePromptFileLog()
        {
            return IsPromptFileLoggingEnabled;
        }

        private static string FormatPromptInputForLog(string? systemPrompt, string userPrompt)
        {
            var normalizedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? string.Empty : systemPrompt.Trim();
            var normalizedUserPrompt = string.IsNullOrWhiteSpace(userPrompt) ? string.Empty : userPrompt.Trim();
            return $"SYSTEM_PROMPT\n{normalizedSystemPrompt}\n\nUSER_PROMPT\n{normalizedUserPrompt}";
        }

        private static string FormatPromptOutputForLog(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return string.Empty;
            }

            if (TryParseJsonObjectFromResponse(output, out var jsonObject))
            {
                return JsonSerializer.Serialize(jsonObject, JsonLogOptions);
            }

            return output.Trim();
        }

        private static bool TryParseJsonObjectFromResponse(string response, out JsonElement objectElement)
        {
            objectElement = default;
            var cleaned = CleanJsonResponse(response);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                objectElement = doc.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            var cleaned = response.Trim();

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || cleaned.StartsWith("```"))
            {
                var startIndex = cleaned.IndexOf('\n');
                if (startIndex >= 0)
                {
                    // Multi-line fenced code block: remove everything up to and including the first newline.
                    cleaned = cleaned[(startIndex + 1)..];
                }
                else
                {
                    // Single-line fenced JSON, e.g. ```json { ... } ``` or ```{ ... } ```.
                    // Strip everything before the first likely JSON payload token.
                    var jsonStart = FindFirstJsonTokenIndex(cleaned);

                    if (jsonStart > 0)
                    {
                        cleaned = cleaned[jsonStart..];
                    }
                }
            }

            if (cleaned.EndsWith("```", StringComparison.Ordinal))
            {
                var lastIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (lastIndex > 0)
                {
                    cleaned = cleaned[..lastIndex];
                }
            }

            return cleaned.Trim();
        }

        private static int FindFirstJsonTokenIndex(string value)
        {
            var objectStart = value.IndexOf('{');
            var arrayStart = value.IndexOf('[');

            if (objectStart >= 0 && arrayStart >= 0)
            {
                return Math.Min(objectStart, arrayStart);
            }

            if (objectStart >= 0)
            {
                return objectStart;
            }

            return arrayStart;
        }

        private static string ResolvePromptVersion(string? version)
        {
            if (!string.IsNullOrWhiteSpace(version) &&
                PromptProfiles.TryGetValue(version.Trim(), out var selectedVersion))
            {
                return selectedVersion;
            }

            return PromptVersionV1;
        }

        private static string BuildAnalysisSystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, AnalysisSystemTemplateName);
        }

        private static string BuildAnalysisUserPrompt(
            string version,
            string schema,
            string data,
            string attachments,
            string? rubric)
        {
            var replacements = new Dictionary<string, string>
            {
                ["SCHEMA"] = schema,
                ["DATA"] = data,
                ["ATTACHMENTS"] = attachments,
                ["RUBRIC"] = ResolveAnalysisRubric(rubric, version)
            };

            return RenderPromptTemplate(version, AnalysisUserTemplateName, replacements);
        }

        private static string ResolveAnalysisRubric(string? providedRubric, string version)
        {
            if (!string.IsNullOrWhiteSpace(providedRubric))
            {
                return providedRubric;
            }

            return GetRequiredPromptTemplate(version, AnalysisRubricTemplateName);
        }

        private static string BuildAttachmentSystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, AttachmentSystemTemplateName);
        }

        private static string BuildAttachmentUserPrompt(string version, string attachment)
        {
            return RenderPromptTemplate(version, AttachmentUserTemplateName, new Dictionary<string, string>
            {
                ["ATTACHMENT"] = attachment
            });
        }

        private static string BuildScoresheetSectionSystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, ScoresheetSystemTemplateName);
        }

        private static string BuildScoresheetSectionUserPrompt(
            string version,
            string data,
            string attachments,
            string section,
            string response)
        {
            return RenderPromptTemplate(version, ScoresheetUserTemplateName, new Dictionary<string, string>
            {
                ["DATA"] = data,
                ["ATTACHMENTS"] = attachments,
                ["SECTION"] = section,
                ["RESPONSE"] = response
            });
        }

        private static bool TryGetPromptTemplate(string version, string templateName, out string template)
        {
            template = string.Empty;
            var cacheKey = $"{version}/{templateName}";
            if (PromptTemplateCache.TryGetValue(cacheKey, out var cachedTemplate))
            {
                template = cachedTemplate;
                return true;
            }

            var path = Path.Combine(AppContext.BaseDirectory, PromptTemplatesFolder, version, $"{templateName}.txt");
            if (!File.Exists(path))
            {
                return false;
            }

            var loaded = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(loaded))
            {
                return false;
            }

            template = loaded;
            PromptTemplateCache[cacheKey] = loaded;
            return true;
        }

        private static string GetRequiredPromptTemplate(string version, string templateName)
        {
            if (TryGetPromptTemplate(version, templateName, out var template))
            {
                return template;
            }

            throw new InvalidOperationException(
                $"Missing required prompt template '{templateName}.txt' for prompt version '{version}'.");
        }

        private static string RenderPromptTemplate(
            string version,
            string templateName,
            IReadOnlyDictionary<string, string> runtimeReplacements)
        {
            return RenderPromptTemplateInternal(
                version,
                templateName,
                runtimeReplacements,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string RenderPromptTemplateInternal(
            string version,
            string templateName,
            IReadOnlyDictionary<string, string> runtimeReplacements,
            ISet<string> resolutionStack)
        {
            if (!resolutionStack.Add(templateName))
            {
                throw new InvalidOperationException(
                    $"Detected cyclic prompt fragment reference while resolving '{templateName}.txt' for prompt version '{version}'.");
            }

            var template = GetRequiredPromptTemplate(version, templateName);
            var replacements = new Dictionary<string, string>(runtimeReplacements, StringComparer.Ordinal);
            var baseTemplateName = GetTemplateBaseName(templateName);

            foreach (var placeholder in GetTemplatePlaceholders(template))
            {
                if (replacements.ContainsKey(placeholder))
                {
                    continue;
                }

                var fragmentTemplateName = ResolveFragmentTemplateName(version, baseTemplateName, placeholder);
                if (!string.IsNullOrWhiteSpace(fragmentTemplateName))
                {
                    replacements[placeholder] = RenderPromptTemplateInternal(
                        version,
                        fragmentTemplateName,
                        new Dictionary<string, string>(StringComparer.Ordinal),
                        resolutionStack);
                }
            }

            var rendered = template;
            foreach (var replacement in replacements)
            {
                rendered = rendered.Replace($"{{{{{replacement.Key}}}}}", replacement.Value ?? string.Empty, StringComparison.Ordinal);
            }

            var unresolved = GetTemplatePlaceholders(rendered);
            if (unresolved.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Unresolved prompt placeholders in '{templateName}.txt' for prompt version '{version}': {string.Join(", ", unresolved.OrderBy(item => item))}");
            }

            resolutionStack.Remove(templateName);
            return rendered;
        }

        private static string? ResolveFragmentTemplateName(string version, string baseTemplateName, string placeholderName)
        {
            var normalizedPlaceholder = placeholderName.ToLowerInvariant();
            var baseScopedCandidate = $"{baseTemplateName}.{normalizedPlaceholder}";
            if (TryGetPromptTemplate(version, baseScopedCandidate, out _))
            {
                return baseScopedCandidate;
            }

            if (TryResolveCommonTemplateName(placeholderName, out var commonTemplateName))
            {
                if (TryGetPromptTemplate(version, commonTemplateName, out _))
                {
                    return commonTemplateName;
                }
            }

            return null;
        }

        private static bool TryResolveCommonTemplateName(string placeholderName, out string commonTemplateName)
        {
            commonTemplateName = string.Empty;
            if (!placeholderName.StartsWith("COMMON_", StringComparison.Ordinal))
            {
                return false;
            }

            var suffix = placeholderName.Substring("COMMON_".Length).ToLowerInvariant();
            suffix = suffix.Replace('_', '.');
            commonTemplateName = $"common.{suffix}";
            return true;
        }

        private static string GetTemplateBaseName(string templateName)
        {
            var separatorIndex = templateName.IndexOf('.', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                return templateName;
            }

            return templateName.Substring(0, separatorIndex);
        }

        private static ISet<string> GetTemplatePlaceholders(string template)
        {
            var placeholders = new HashSet<string>(StringComparer.Ordinal);
            var searchIndex = 0;

            while (searchIndex < template.Length)
            {
                var start = template.IndexOf("{{", searchIndex, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = template.IndexOf("}}", start + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    break;
                }

                var placeholder = template.Substring(start + 2, end - start - 2).Trim();
                if (!string.IsNullOrWhiteSpace(placeholder))
                {
                    placeholders.Add(placeholder);
                }

                searchIndex = end + 2;
            }

            return placeholders;
        }

        private static string ExtractSummaryFromJson(string output)
        {
            if (!TryParseJsonObjectFromResponse(output, out var jsonObject))
            {
                return output?.Trim() ?? string.Empty;
            }

            if (jsonObject.TryGetProperty(AIJsonKeys.Summary, out var summaryProp) &&
                summaryProp.ValueKind == JsonValueKind.String)
            {
                return summaryProp.GetString() ?? string.Empty;
            }

            return output?.Trim() ?? string.Empty;
        }
    }
}
