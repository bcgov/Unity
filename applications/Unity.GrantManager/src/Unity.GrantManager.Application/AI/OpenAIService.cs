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

        private string? ApiKey => _configuration["Azure:OpenAI:ApiKey"];
        private string? ApiUrl => _configuration["Azure:OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
        private readonly string MissingApiKeyMessage = "OpenAI API key is not configured";

        // Optional local debugging sink for prompt payload logs to a local file.
        // Not intended for deployed/shared environments.
        private bool IsPromptFileLoggingEnabled => _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
        private const string PromptLogDirectoryName = "logs";
        private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

        private static readonly JsonSerializerOptions JsonLogOptions = new() { WriteIndented = true };

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

        public Task<string> GenerateCompletionAsync(AICompletionRequest request)
        {
            return GenerateSummaryAsync(
                request?.UserPrompt ?? string.Empty,
                request?.SystemPrompt,
                request?.MaxTokens ?? 150);
        }

        public Task<string> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request)
        {
            return GenerateAttachmentSummaryAsync(
                request?.FileName ?? string.Empty,
                request?.FileContent ?? Array.Empty<byte>(),
                request?.ContentType ?? "application/octet-stream");
        }

        public Task<string> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request)
        {
            var dataJson = JsonSerializer.Serialize(request.Data, new JsonSerializerOptions { WriteIndented = true });
            var schemaJson = JsonSerializer.Serialize(request.Schema, new JsonSerializerOptions { WriteIndented = true });

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();

            var applicationContent = $@"DATA
{dataJson}";

            var formFieldConfiguration = $@"SCHEMA
{schemaJson}";

            return AnalyzeApplicationAsync(
                applicationContent,
                attachmentSummaries,
                request.Rubric ?? string.Empty,
                formFieldConfiguration);
        }

        public Task<string> GenerateScoresheetSectionAnswersAsync(ScoresheetSectionRequest request)
        {
            var dataJson = JsonSerializer.Serialize(request.Data, new JsonSerializerOptions { WriteIndented = true });
            var sectionJson = JsonSerializer.Serialize(request.SectionSchema, new JsonSerializerOptions { WriteIndented = true });

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();

            return GenerateScoresheetSectionAnswersAsync(
                dataJson,
                attachmentSummaries,
                sectionJson,
                request.SectionName);
        }

        public async Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return "AI analysis not available - service not configured.";
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
                    return "AI analysis failed - service temporarily unavailable.";
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return "No summary generated.";
                }

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var choices = jsonDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    return message.GetProperty("content").GetString() ?? "No summary generated.";
                }

                return "No summary generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary");
                return "AI analysis failed - please try again later.";
            }
        }

        public async Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType)
        {
            try
            {
                var extractedText = await _textExtractionService.ExtractTextAsync(fileName, fileContent, contentType);

                var prompt = @"ROLE
You are a professional grant analyst for the BC Government.

TASK
Produce a concise reviewer-facing summary of the provided attachment context.

OUTPUT
- Plain text only
- 1-2 complete sentences

RULES
- Use only the provided attachment context as evidence.
- If text content is present, summarize the actual content.
- If text content is missing or empty, provide a conservative metadata-based summary.
- Do not invent missing details.
- Keep the summary specific, concrete, and reviewer-facing.
- Return plain text only (no markdown, bullets, or JSON).";

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
                var contentToAnalyze = $"ATTACHMENT\n{JsonSerializer.Serialize(attachmentPayload, JsonLogOptions)}";

                await LogPromptInputAsync("AttachmentSummary", prompt, contentToAnalyze);
                var modelOutput = await GenerateSummaryAsync(contentToAnalyze, prompt, 150);
                await LogPromptOutputAsync("AttachmentSummary", modelOutput);
                return modelOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attachment summary for {FileName}", fileName);
                return $"AI analysis not available for this attachment ({fileName}).";
            }
        }

        public async Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric, string? formFieldConfiguration = null)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage);
                return "AI analysis not available - service not configured.";
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
                    catch (JsonException)
                    {
                        _logger.LogWarning("Invalid form field configuration JSON. Using empty schema payload.");
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

                var analysisContent = $@"SCHEMA
{JsonSerializer.Serialize(schemaPayload, JsonLogOptions)}

DATA
{JsonSerializer.Serialize(dataPayload, JsonLogOptions)}

ATTACHMENTS
{JsonSerializer.Serialize(attachmentsPayload, JsonLogOptions)}

RUBRIC
{rubric}

SEVERITY
ERROR: Issue that would likely prevent the application from being approved.
WARNING: Issue that could negatively affect the application's approval.
RECOMMENDATION: Reviewer-facing improvement or follow-up consideration.

SCORE
HIGH: Application demonstrates strong evidence across most rubric areas with few or no issues.
MEDIUM: Application has some gaps or weaknesses that require reviewer attention.
LOW: Application has significant gaps or risks across key rubric areas.

OUTPUT
{{
  ""overall_score"": ""HIGH/MEDIUM/LOW"",
  ""warnings"": [
    {{
      ""category"": ""Brief summary of the warning"",
      ""message"": ""Detailed warning message with full context and explanation""
    }}
  ],
  ""errors"": [
    {{
      ""category"": ""Brief summary of the error"",
      ""message"": ""Detailed error message with full context and explanation""
    }}
  ],
  ""recommendations"": [
    {{
      ""category"": ""Brief summary of the recommendation"",
      ""message"": ""Detailed recommendation with specific actionable guidance""
    }}
  ]
}}

RULES
- Use only SCHEMA, DATA, ATTACHMENTS, and RUBRIC as evidence.
- Do not invent fields, documents, requirements, or facts.
- Treat missing or empty values as findings only when they weaken rubric evidence.
- Prefer material issues; avoid nitpicking.
- Each error/warning/recommendation must describe one concrete issue or consideration and why it matters.
- Use 3-6 words for category.
- Each message must be 1-2 complete sentences.
- Each message must be grounded in concrete evidence from provided inputs.
- If attachment evidence is used, reference the attachment explicitly in the message.
- Do not provide applicant-facing advice.
- Do not mention rubric section names in findings.
- If no findings exist, return empty arrays.
- overall_score must be HIGH, MEDIUM, or LOW.
- Return values exactly as specified in OUTPUT.
- Do not return keys outside OUTPUT.
- Return valid JSON only.
- Return plain JSON only (no markdown).";

                var systemPrompt = @"ROLE
You are an expert grant analyst assistant for human reviewers.

TASK
Using SCHEMA, DATA, ATTACHMENTS, RUBRIC, SEVERITY, SCORE, OUTPUT, and RULES, return review findings.";

                await LogPromptInputAsync("ApplicationAnalysis", systemPrompt, analysisContent);
                var rawAnalysis = await GenerateSummaryAsync(analysisContent, systemPrompt, 1000);
                await LogPromptOutputAsync("ApplicationAnalysis", rawAnalysis);

                // Post-process the AI response to add unique IDs to errors and warnings
                return AddIdsToAnalysisItems(rawAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing application");
                return "AI analysis failed - please try again later.";
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
                        if (property.Name == "errors" || property.Name == "warnings")
                        {
                            writer.WritePropertyName(property.Name);
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
                            property.WriteTo(writer);
                        }
                    }

                    // Add dismissed_items array if not present
                    if (!jsonDoc.RootElement.TryGetProperty("dismissed_items", out _))
                    {
                        writer.WritePropertyName("dismissed_items");
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

        public async Task<string> GenerateScoresheetAnswersAsync(string applicationContent, List<string> attachmentSummaries, string scoresheetQuestions)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage); 
                return "{}";
            }

            try
            {
                var attachmentSummariesText = attachmentSummaries?.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}

SCORESHEET QUESTIONS:
{scoresheetQuestions}

Please analyze this grant application and provide appropriate answers for each scoresheet question.

For numeric questions, provide a numeric value within the specified range.
For yes/no questions, provide either 'Yes' or 'No'.
For text questions, provide a concise, relevant response.
For select list questions, choose the most appropriate option from the provided choices.
For text area questions, provide a detailed but concise response.

Base your answers on the application content and attachment summaries provided. Be objective and fair in your assessment.

Return your response as a JSON object where each key is the question ID and the value is the appropriate answer:
{{
  ""question-id-1"": ""answer-value-1"",
  ""question-id-2"": ""answer-value-2""
}}
Do not return any markdown formatting, just the JSON by itself";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government.
Analyze the provided application and generate appropriate answers for the scoresheet questions based on the application content.
Be thorough, objective, and fair in your assessment. Base your answers strictly on the provided application content.
Respond only with valid JSON in the exact format requested.";

                await LogPromptInputAsync("ScoresheetAll", systemPrompt, analysisContent);
                var modelOutput = await GenerateSummaryAsync(analysisContent, systemPrompt, 2000);
                await LogPromptOutputAsync("ScoresheetAll", modelOutput);
                return modelOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet answers");
                return "{}";
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
                var attachmentSummariesText = attachmentSummaries?.Count > 0
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

                var analysisContent = $@"DATA
{applicationContent}

ATTACHMENTS
- {attachmentSummariesText}

SECTION
{JsonSerializer.Serialize(sectionPayload, JsonLogOptions)}

RESPONSE
{{
  ""<question_id>"": {{
    ""answer"": ""<string | number>"",
    ""rationale"": ""<evidence-based rationale>"",
    ""confidence"": 85
  }}
}}

RULES
- Use only DATA and ATTACHMENTS as evidence.
- Do not invent missing application details.
- Return exactly one answer object per question ID in SECTION.questions.
- Do not omit any question IDs from SECTION.questions.
- Do not add keys that are not question IDs from SECTION.questions.
- Use RESPONSE as the output contract and fill every placeholder value.
- Each answer object must include: answer, rationale, confidence.
- answer type must match question type: Number => numeric; YesNo/SelectList/Text/TextArea => string.
- For yes/no questions, answer must be exactly ""Yes"" or ""No"".
- For numeric questions, answer must be a numeric value within the allowed range.
- For select list questions, answer must be the selected availableOptions.number encoded as a string.
- For select list questions, never return option label text (for example: ""Yes"", ""No"", or ""N/A""); return the option number string.
- For text and text area questions, answer must be concise, grounded in evidence, and non-empty.
- rationale must be 1-2 complete sentences grounded in concrete DATA/ATTACHMENTS evidence.
- For every question, rationale must justify both the selected answer and confidence level based on evidence strength.
- If evidence is insufficient, choose the most conservative valid answer and state uncertainty in rationale.
- confidence must be an integer from 0 to 100.
- Confidence reflects certainty in the selected answer given available evidence, not application quality.
- Return values exactly as specified in RESPONSE.
- Do not return keys outside RESPONSE.
- Return valid JSON only.
- Return plain JSON only (no markdown).";

                var systemPrompt = @"ROLE
You are an expert grant application reviewer for the BC Government.

TASK
Using DATA, ATTACHMENTS, SECTION, RESPONSE, and RULES, answer only the questions in SECTION.";

                await LogPromptInputAsync("ScoresheetSection", systemPrompt, analysisContent);
                var modelOutput = await GenerateSummaryAsync(analysisContent, systemPrompt, 2000);
                await LogPromptOutputAsync("ScoresheetSection", modelOutput);
                return modelOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet section answers for section {SectionName}", sectionName);
                return "{}";
            }
        }

        private async Task LogPromptInputAsync(string promptType, string? systemPrompt, string userPrompt)
        {
            var formattedInput = FormatPromptInputForLog(systemPrompt, userPrompt);
            _logger.LogInformation("AI {PromptType} input payload: {PromptInput}", promptType, formattedInput);
            await WritePromptLogFileAsync(promptType, "INPUT", formattedInput);
        }

        private async Task LogPromptOutputAsync(string promptType, string output)
        {
            var formattedOutput = FormatPromptOutputForLog(output);
            _logger.LogInformation("AI {PromptType} model output payload: {ModelOutput}", promptType, formattedOutput);
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
    }
}
