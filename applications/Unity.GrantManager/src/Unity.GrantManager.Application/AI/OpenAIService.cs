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

                string contentToAnalyze;
                string prompt;

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogDebug("Extracted {TextLength} characters from {FileName}", extractedText.Length, fileName);

                    contentToAnalyze = $"Document: {fileName}\nType: {contentType}\nContent:\n{extractedText}";
                    prompt = "Please analyze this document and provide a concise summary of its content, purpose, and key information, for use by your fellow grant analysts. It should be 1-2 sentences long and about 46 tokens.";
                }
                else
                {
                    _logger.LogDebug("No text extracted from {FileName}, analyzing metadata only", fileName);

                    contentToAnalyze = $"File: {fileName}, Type: {contentType}, Size: {fileContent.Length} bytes";
                    prompt = "Please analyze this document and provide a concise summary of its content, purpose, and key information, for use by your fellow grant analysts. It should be 1-2 sentences long and about 46 tokens.";
                }

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
                var attachmentSummariesText = attachmentSummaries?.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                var fieldConfigurationSection = !string.IsNullOrEmpty(formFieldConfiguration)
                    ? $@"
{formFieldConfiguration}"
                    : string.Empty;

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}
{fieldConfigurationSection}

EVALUATION RUBRIC:
{rubric}

Analyze this grant application comprehensively across all five rubric categories (Eligibility, Completeness, Financial Review, Risk Assessment, and Quality Indicators). Identify issues, concerns, and areas for improvement. Return your findings in the following JSON format:
{{
  ""overall_score"": ""HIGH/MEDIUM/LOW"",
  ""warnings"": [
    {{
      ""category"": ""Brief summary of the warning"",
      ""message"": ""Detailed warning message with full context and explanation"",
      ""severity"": ""WARNING""
    }}
  ],
  ""errors"": [
    {{
      ""category"": ""Brief summary of the error"",
      ""message"": ""Detailed error message with full context and explanation"",
      ""severity"": ""ERROR""
    }}
  ],
  ""recommendations"": [
    {{
      ""category"": ""Brief summary of the recommendation"",
      ""message"": ""Detailed recommendation with specific actionable guidance""
    }}
  ]
}}

Important: The 'category' field should be a concise summary (3-6 words) that captures the essence of the issue, while the 'message' field should contain the detailed explanation.";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government.

Conduct a thorough, comprehensive analysis across all rubric categories. Identify substantive issues, concerns, and opportunities for improvement.

Classify findings based on their impact on the application's evaluation and fundability:
- ERRORS: Important missing information, significant gaps in required content, compliance issues, or major concerns affecting eligibility
- WARNINGS: Areas needing clarification, moderate issues, or concerns that should be addressed

Evaluate the quality, clarity, and appropriateness of all application content. Be thorough but fair - identify real issues while avoiding nitpicking.

Respond only with valid JSON in the exact format requested.";

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

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}

SCORESHEET SECTION: {sectionName}
{sectionJson}

Please analyze this grant application and provide appropriate answers for each question in the ""{sectionName}"" section only.

For each question, provide:
1. Your answer based on the application content
2. A brief cited description (1-2 sentences) explaining your reasoning with specific references to the application content
3. A confidence score from 0-100 indicating how confident you are in your answer based on available information

Guidelines for answers:
- For numeric questions, provide a numeric value within the specified range
- For yes/no questions, provide either 'Yes' or 'No'
- For text questions, provide a concise, relevant response
- For select list questions, respond with ONLY the number from the 'number' field (1, 2, 3, etc.) of your chosen option. NEVER return 0 - the lowest valid answer is 1. For example: if you want '(0 pts) No outcomes provided', choose the option where number=1, not 0.
- For text area questions, provide a detailed but concise response
- Base your confidence score on how clearly the application content supports your answer

Return your response as a JSON object where each key is the question ID and the value contains the answer, citation, and confidence:
{{
  ""question-id-1"": {{
    ""answer"": ""your-answer-here"",
    ""citation"": ""Brief explanation with specific reference to application content"",
    ""confidence"": 85
  }},
  ""question-id-2"": {{
    ""answer"": ""3"",
    ""citation"": ""Based on the project budget of $50,000 mentioned in the application, this falls into the medium budget category"",
    ""confidence"": 90
  }}
}}

IMPORTANT FOR SELECT LIST QUESTIONS: If a question has availableOptions like:
[{{""number"":1,""value"":""Low (Under $25K)""}}, {{""number"":2,""value"":""Medium ($25K-$75K)""}}, {{""number"":3,""value"":""High (Over $75K)""}}]
Then respond with ONLY the number (e.g., ""3"" for ""High (Over $75K)""), not the text value.

Do not return any markdown formatting, just the JSON by itself";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government.
Analyze the provided application and generate appropriate answers for the scoresheet section questions based on the application content.
Be thorough, objective, and fair in your assessment. Base your answers strictly on the provided application content.
Always provide citations that reference specific parts of the application content to support your answers.
Be honest about your confidence level - if information is missing or unclear, reflect this in a lower confidence score.
Respond only with valid JSON in the exact format requested.";

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
