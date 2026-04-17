using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Models;
using Unity.AI.Prompts;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime
{
    [ExposeServices(typeof(IAIService))]
    public class OpenAIRuntimeService : IAIService, ITransientDependency
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIRuntimeService> _logger;
        private readonly ITextExtractionService _textExtractionService;
        private readonly OpenAITransportService _openAITransportService;
        private readonly OpenAIConfigurationResolver _openAIConfigurationResolver;
        private const string ApplicationAnalysisPromptType = AIPromptTypes.ApplicationAnalysis;
        private const string AttachmentSummaryPromptType = AIPromptTypes.AttachmentSummary;
        private const string ApplicationScoringPromptType = AIPromptTypes.ApplicationScoring;
        private const string AIServiceNotConfiguredMessage = "AI service not available - service not configured.";
        private const string AIServiceTemporarilyUnavailableMessage = "AI request failed - service temporarily unavailable.";
        private const string AIRequestFailedRetryMessage = "AI request failed - please try again later.";
        private const int MaxAiAttempts = 3;
        private const int DefaultCompletionTokens = 2000;
        private const int DefaultAttachmentSummaryCompletionTokens = 2000;
        private const int DefaultApplicationAnalysisCompletionTokens = 4000;
        private const int DefaultApplicationScoringCompletionTokens = 8000;

        private int AttachmentSummaryCompletionTokens => _openAIConfigurationResolver.ResolveCompletionTokens(AttachmentSummaryPromptType, DefaultAttachmentSummaryCompletionTokens);
        private int ApplicationAnalysisCompletionTokens => _openAIConfigurationResolver.ResolveCompletionTokens(ApplicationAnalysisPromptType, DefaultApplicationAnalysisCompletionTokens);
        private int ApplicationScoringCompletionTokens => _openAIConfigurationResolver.ResolveCompletionTokens(ApplicationScoringPromptType, DefaultApplicationScoringCompletionTokens);
        private readonly string MissingApiKeyMessage = "OpenAI API key is not configured";

        // Optional local debugging sink for prompt payload logs to a local file.
        // Not intended for deployed/shared environments.
        private bool IsPromptFileLoggingEnabled => _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
        private const string PromptLogDirectoryName = "logs";
        private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

        private static readonly JsonSerializerOptions JsonLogOptions = new() { WriteIndented = true };

        public OpenAIRuntimeService(
            IConfiguration configuration,
            ILogger<OpenAIRuntimeService> logger,
            ITextExtractionService textExtractionService,
            OpenAITransportService openAITransportService,
            OpenAIConfigurationResolver openAIConfigurationResolver)
        {
            _configuration = configuration;
            _logger = logger;
            _textExtractionService = textExtractionService;
            _openAITransportService = openAITransportService;
            _openAIConfigurationResolver = openAIConfigurationResolver;
        }

        public Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrEmpty(_openAIConfigurationResolver.ResolveApiKey()))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public async Task<AICompletionResponse> GenerateCompletionAsync(AICompletionRequest request)
        {
            var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                request?.UserPrompt ?? string.Empty,
                null,
                request?.MaxTokens ?? DefaultCompletionTokens,
                request?.Temperature),
                AIProviderPayloadValidator.IsValidAttachmentSummaryText,
                "completion");
            return new AICompletionResponse { Content = ResolveNarrativeContent(result) };
        }

        public async Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(ApplicationAnalysisPromptType));
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
            var systemPrompt = OpenAIPromptRenderer.BuildApplicationAnalysisSystemPrompt(promptVersion);
            var applicationAnalysisContent = OpenAIPromptRenderer.BuildApplicationAnalysisUserPrompt(
                promptVersion,
                schema,
                data,
                attachments);
            await LogPromptInputAsync(ApplicationAnalysisPromptType, promptVersion, systemPrompt, applicationAnalysisContent);
            var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    applicationAnalysisContent,
                    systemPrompt,
                    ApplicationAnalysisCompletionTokens,
                    operationName: ApplicationAnalysisPromptType,
                    promptVersion: promptVersion),
                AIProviderPayloadValidator.IsValidApplicationAnalysisJson,
                "application analysis");
            await LogPromptOutputAsync(ApplicationAnalysisPromptType, promptVersion, result.CaptureOutput);

            if (result.Outcome != AIOperationOutcome.Success)
            {
                return new ApplicationAnalysisResponse();
            }

            return OpenAIResponseParser.ParseApplicationAnalysisResponse(result.Content);
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var fileName = request.FileName ?? string.Empty;
            var fileContent = request.FileContent ?? Array.Empty<byte>();
            var contentType = request.ContentType ?? "application/octet-stream";
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(AttachmentSummaryPromptType));

            try
            {
                var extractedText = await _textExtractionService.ExtractTextAsync(fileName, fileContent, contentType);
                var prompt = OpenAIPromptRenderer.BuildAttachmentSummarySystemPrompt(promptVersion);

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
                var contentToAnalyze = OpenAIPromptRenderer.BuildAttachmentSummaryUserPrompt(promptVersion, attachment);

                await LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, prompt, contentToAnalyze);
            var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    contentToAnalyze,
                    prompt,
                    AttachmentSummaryCompletionTokens,
                    operationName: AttachmentSummaryPromptType,
                    promptVersion: promptVersion,
                    fileName: fileName),
                    AIProviderPayloadValidator.IsValidAttachmentSummaryText,
                    "attachment summary");
                await LogPromptOutputAsync(AttachmentSummaryPromptType, promptVersion, result.CaptureOutput);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new AttachmentSummaryResponse
                    {
                        Summary = $"AI analysis not available for this attachment ({fileName})."
                    };
                }

                return new AttachmentSummaryResponse
                {
                    Summary = ExtractSummaryFromJson(result.Content)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attachment summary for {FileName}", fileName);
                return new AttachmentSummaryResponse
                {
                    Summary = $"AI analysis not available for this attachment ({fileName})."
                };
            }
        }

        public async Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(ApplicationScoringPromptType));
            var dataJson = JsonSerializer.Serialize(request.Data, JsonLogOptions);
            var sectionJson = JsonSerializer.Serialize(request.SectionSchema, JsonLogOptions);

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();
            if (string.IsNullOrEmpty(_openAIConfigurationResolver.ResolveApiKey(ApplicationScoringPromptType)))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage);
                return new ApplicationScoringResponse();
            }

            try
            {
                var attachments = attachmentSummaries.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((summary, index) => $"Attachment {index + 1}: {summary}"))
                    : "No attachments provided.";

                var section = OpenAIPromptRenderer.BuildAliasedApplicationScoringSection(request.SectionName, sectionJson, out var questionIdAliasMap);
                var response = OpenAIPromptRenderer.BuildApplicationScoringResponseTemplate(section);
                if (response == "{}")
                {
                    _logger.LogWarning(
                        "Skipping AI application scoring for section {SectionName} because response template could not be built from section schema.",
                        request.SectionName);
                    return new ApplicationScoringResponse();
                }

                var applicationScoringContent = OpenAIPromptRenderer.BuildApplicationScoringUserPrompt(
                    promptVersion,
                    dataJson,
                    attachments,
                    section,
                    response);
                var systemPrompt = OpenAIPromptRenderer.BuildApplicationScoringSystemPrompt(promptVersion);

                await LogPromptInputAsync(ApplicationScoringPromptType, promptVersion, systemPrompt, applicationScoringContent);
                var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    applicationScoringContent,
                    systemPrompt,
                    ApplicationScoringCompletionTokens,
                    operationName: ApplicationScoringPromptType,
                    promptVersion: promptVersion),
                    content => AIProviderPayloadValidator.IsValidApplicationScoringJson(content, section),
                    $"application scoring section {request.SectionName}");
                await LogPromptOutputAsync(ApplicationScoringPromptType, promptVersion, result.CaptureOutput);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new ApplicationScoringResponse();
                }

                return OpenAIResponseParser.ParseApplicationScoringResponse(result.Content, questionIdAliasMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application scoring answers for section {SectionName}", request.SectionName);
                return new ApplicationScoringResponse();
            }
        }

        private async Task<AIOperationResult> GenerateWithRetryAsync(
            Func<Task<AIOperationResult>> operation,
            Func<string, bool> validator,
            string operationName)
        {
            var lastResult = AIOperationResult.InvalidOutput();

            for (var attempt = 1; attempt <= MaxAiAttempts; attempt++)
            {
                lastResult = await operation();

                if (lastResult.Outcome == AIOperationOutcome.Success && validator(lastResult.Content))
                {
                    return lastResult;
                }

                if (lastResult.Outcome == AIOperationOutcome.Success)
                {
                    lastResult = lastResult.WithOutcome(AIOperationOutcome.InvalidOutput);
                }

                if (lastResult.Outcome == AIOperationOutcome.PermanentFailure)
                {
                    return lastResult;
                }

                if (attempt < MaxAiAttempts)
                {
                    if (lastResult.Outcome == AIOperationOutcome.TransientFailure)
                    {
                        _logger.LogWarning(
                            "AI {OperationName} attempt {Attempt}/{MaxAttempts} failed transiently; retrying",
                            operationName,
                            attempt,
                            MaxAiAttempts);
                    }
                    else if (lastResult.Outcome == AIOperationOutcome.InvalidOutput)
                    {
                        _logger.LogWarning(
                            "AI {OperationName} attempt {Attempt}/{MaxAttempts} returned invalid response shape; retrying",
                            operationName,
                            attempt,
                            MaxAiAttempts);
                    }
                }
            }

            _logger.LogWarning(
                "AI {OperationName} exhausted retries with outcome {Outcome}; returning last result",
                operationName,
                lastResult.Outcome);
            return lastResult;
        }

        private static string ResolveNarrativeContent(AIOperationResult result)
        {
            return result.Outcome switch
            {
                AIOperationOutcome.Success => result.Content,
                AIOperationOutcome.PermanentFailure => AIServiceNotConfiguredMessage,
                AIOperationOutcome.TransientFailure => AIServiceTemporarilyUnavailableMessage,
                _ => AIRequestFailedRetryMessage
            };
        }

        private static int? TryGetInt32(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out var value)
                ? value
                : null;
        }

        private string? ResolvePromptVersionSetting(string operationName)
        {
            var operationPromptVersion = _configuration[$"Azure:Operations:{operationName}:PromptVersion"];
            if (!string.IsNullOrWhiteSpace(operationPromptVersion))
            {
                return operationPromptVersion;
            }

            var defaultPromptVersion = _configuration["Azure:Operations:Defaults:PromptVersion"];
            if (!string.IsNullOrWhiteSpace(defaultPromptVersion))
            {
                return defaultPromptVersion;
            }

            return _configuration["Azure:OpenAI:PromptVersion"];
        }

        private async Task LogPromptInputAsync(string promptType, string promptVersion, string? systemPrompt, string userPrompt)
        {
            var formattedInput = FormatPromptInputForLog(systemPrompt, userPrompt);
            _logger.LogInformation("AI {PromptType} ({PromptVersion}) input payload: {PromptInput}", promptType, promptVersion, formattedInput);
            await WritePromptLogFileAsync(promptType, promptVersion, "INPUT", formattedInput);
        }

        private async Task LogPromptOutputAsync(string promptType, string promptVersion, string output)
        {
            var formattedOutput = FormatPromptOutputForLog(output);
            _logger.LogInformation("AI {PromptType} ({PromptVersion}) model output payload: {ModelOutput}", promptType, promptVersion, formattedOutput);
            await WritePromptLogFileAsync(promptType, promptVersion, "OUTPUT", formattedOutput);
        }

        private async Task WritePromptLogFileAsync(string promptType, string promptVersion, string payloadType, string payload)
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
                var entry = $"{now} [{promptType}] [{promptVersion}] {payloadType}\n{payload}\n\n";
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

            if (TryFormatProviderOutput(output, out var formattedProviderOutput))
            {
                return formattedProviderOutput;
            }

            if (TryParseJsonObjectFromResponse(output, out var jsonObject))
            {
                return JsonSerializer.Serialize(jsonObject, JsonLogOptions);
            }

            return output.Trim();
        }

        private static bool TryFormatProviderOutput(string output, out string formattedOutput)
        {
            formattedOutput = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(output);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("choices", out var choices)
                    || choices.ValueKind != JsonValueKind.Array
                    || choices.GetArrayLength() == 0)
                {
                    return false;
                }

                var firstChoice = choices[0];
                var content = TryGetChoiceContent(firstChoice);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return false;
                }

                var lines = new List<string>();

                if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                {
                    var promptTokens = TryGetInt32(usage, "prompt_tokens");
                    var completionTokens = TryGetInt32(usage, "completion_tokens");
                    int? reasoningTokens = null;

                    if (usage.TryGetProperty("completion_tokens_details", out var completionTokenDetails)
                        && completionTokenDetails.ValueKind == JsonValueKind.Object)
                    {
                        reasoningTokens = TryGetInt32(completionTokenDetails, "reasoning_tokens");
                    }

                    if (promptTokens.HasValue)
                    {
                        lines.Add($"PromptTokens: {promptTokens.Value}");
                    }

                    if (completionTokens.HasValue)
                    {
                        lines.Add($"CompletionTokens: {completionTokens.Value}");
                    }

                    if (reasoningTokens.HasValue)
                    {
                        lines.Add($"ReasoningTokens: {reasoningTokens.Value}");
                    }
                }

                var normalizedContent = FormatPromptOutputContent(content);
                lines.Add("Output:");
                lines.Add(normalizedContent);
                formattedOutput = string.Join(Environment.NewLine, lines);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string? TryGetChoiceContent(JsonElement firstChoice)
        {
            if (!firstChoice.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!message.TryGetProperty("content", out var contentProp) || contentProp.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return contentProp.GetString();
        }

        private static string FormatPromptOutputContent(string content)
        {
            if (TryParseJsonObjectFromResponse(content, out var contentObject))
            {
                return JsonSerializer.Serialize(contentObject, JsonLogOptions);
            }

            return content.Trim();
        }

        private static bool TryParseJsonObjectFromResponse(string response, out JsonElement objectElement)
        {
            objectElement = default;
            var cleaned = AIResponseJson.CleanJsonResponse(response);
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
