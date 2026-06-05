using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Models;
using Unity.AI.Prompts;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime
{
    [ExposeServices(typeof(IAIService))]
    public class OpenAIRuntimeService : IAIService, ITransientDependency
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIRuntimeService> _logger;
        private readonly OpenAITransportService _openAITransportService;
        private readonly OpenAIConfigurationResolver _openAIConfigurationResolver;
        private const string ApplicationAnalysisPromptType = AIPromptTypes.ApplicationAnalysis;
        private const string AttachmentSummaryPromptType = AIPromptTypes.AttachmentSummary;
        private const string ApplicationScoringPromptType = AIPromptTypes.ApplicationScoring;
        private const int MaxAiAttempts = 3;

        // Optional local debugging sink for prompt payload logs to a local file.
        // Not intended for deployed/shared environments.
        private bool IsPromptFileLoggingEnabled => _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
        private const string PromptLogDirectoryName = "logs";
        private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";



        public OpenAIRuntimeService(
            IConfiguration configuration,
            ILogger<OpenAIRuntimeService> logger,
            OpenAITransportService openAITransportService,
            OpenAIConfigurationResolver openAIConfigurationResolver)
        {
            _configuration = configuration;
            _logger = logger;
            _openAITransportService = openAITransportService;
            _openAIConfigurationResolver = openAIConfigurationResolver;
        }

        public Task<bool> IsAvailableAsync()
        {
            try
            {
                _openAIConfigurationResolver.ResolveApiKey();
                return Task.FromResult(true);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI is unavailable because the OpenAI configuration could not be resolved.");
                return Task.FromResult(false);
            }
        }

        public async Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(
                request.PromptVersion ?? _openAIConfigurationResolver.ResolvePromptVersion(ApplicationAnalysisPromptType));
            var data = JsonSerializer.Serialize(request.Data, AIJsonDefaults.Indented);
            var schema = JsonSerializer.Serialize(request.Schema, AIJsonDefaults.Indented);

            var attachmentsPayload = request.Attachments
                .Select(a => new
                {
                    name = string.IsNullOrWhiteSpace(a.Name) ? "attachment" : a.Name.Trim(),
                    summary = string.IsNullOrWhiteSpace(a.Summary) ? string.Empty : a.Summary.Trim()
                })
                .Cast<object>();

            var attachments = JsonSerializer.Serialize(attachmentsPayload, AIJsonDefaults.Indented);
            var systemPrompt = OpenAIPromptRenderer.BuildApplicationAnalysisSystemPrompt(promptVersion);
            var applicationAnalysisContent = OpenAIPromptRenderer.BuildApplicationAnalysisUserPrompt(
                promptVersion,
                schema,
                data,
                attachments);
            await LogPromptInputAsync(ApplicationAnalysisPromptType, promptVersion, systemPrompt, applicationAnalysisContent, cancellationToken);
            var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    applicationAnalysisContent,
                    systemPrompt,
                    _openAIConfigurationResolver.ResolveCompletionTokens(ApplicationAnalysisPromptType),
                    operationName: ApplicationAnalysisPromptType,
                    promptVersion: promptVersion,
                    cancellationToken: cancellationToken),
                AIProviderPayloadValidator.IsValidApplicationAnalysisJson,
                "application analysis",
                cancellationToken);

            if (result.Outcome != AIOperationOutcome.Success)
            {
                return new ApplicationAnalysisResponse();
            }

            await LogPromptOutputAsync(ApplicationAnalysisPromptType, promptVersion, result.CaptureOutput, cancellationToken);
            return OpenAIResponseParser.ParseApplicationAnalysisResponse(result.Content);
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var fileName = request.FileName ?? string.Empty;
            var contentType = request.ContentType ?? "application/octet-stream";
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(
                request.PromptVersion ?? _openAIConfigurationResolver.ResolvePromptVersion(AttachmentSummaryPromptType));

            try
            {
                var extractedText = request.ExtractedText;
                var prompt = OpenAIPromptRenderer.BuildAttachmentSummarySystemPrompt(promptVersion);

                var attachmentText = string.IsNullOrWhiteSpace(extractedText) ? null : extractedText;
                if (attachmentText != null)
                {
                    _logger.LogDebug("Received {TextLength} extracted characters for {FileName}", attachmentText.Length, fileName);
                }
                else
                {
                    _logger.LogDebug("No text extracted from {FileName}, analyzing metadata only", fileName);
                }

                var attachmentPayload = new
                {
                    name = fileName,
                    contentType,
                    text = attachmentText
                };
                var attachment = JsonSerializer.Serialize(attachmentPayload, AIJsonDefaults.Indented);
                var contentToAnalyze = OpenAIPromptRenderer.BuildAttachmentSummaryUserPrompt(promptVersion, attachment);

                await LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, prompt, contentToAnalyze, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        contentToAnalyze,
                        prompt,
                        _openAIConfigurationResolver.ResolveCompletionTokens(AttachmentSummaryPromptType),
                        operationName: AttachmentSummaryPromptType,
                        promptVersion: promptVersion,
                        fileName: fileName,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.IsValidAttachmentSummaryText,
                    "attachment summary",
                    cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new AttachmentSummaryResponse
                    {
                        Summary = $"AI analysis not available for this attachment ({fileName})."
                    };
                }

                await LogPromptOutputAsync(AttachmentSummaryPromptType, promptVersion, result.CaptureOutput, cancellationToken);
                return new AttachmentSummaryResponse
                {
                    Summary = ExtractSummaryFromJson(result.Content)
                };
            }
            catch (OperationCanceledException)
            {
                throw;
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

        public async Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(
                request.PromptVersion ?? _openAIConfigurationResolver.ResolvePromptVersion(ApplicationScoringPromptType));
            var dataJson = JsonSerializer.Serialize(request.Data, AIJsonDefaults.Indented);
            var sectionJson = JsonSerializer.Serialize(request.SectionSchema, AIJsonDefaults.Indented);

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();
            try
            {
                var attachments = attachmentSummaries.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((summary, index) => $"Attachment {index + 1}: {summary}"))
                    : "[]";

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

                await LogPromptInputAsync(ApplicationScoringPromptType, promptVersion, systemPrompt, applicationScoringContent, cancellationToken);
                var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    applicationScoringContent,
                    systemPrompt,
                    _openAIConfigurationResolver.ResolveCompletionTokens(ApplicationScoringPromptType),
                    operationName: ApplicationScoringPromptType,
                    promptVersion: promptVersion,
                    cancellationToken: cancellationToken),
                    content => AIProviderPayloadValidator.IsValidApplicationScoringJson(content, section),
                    $"application scoring section {request.SectionName}",
                    cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new ApplicationScoringResponse();
                }

                await LogPromptOutputAsync(ApplicationScoringPromptType, promptVersion, result.CaptureOutput, cancellationToken);
                return OpenAIResponseParser.ParseApplicationScoringResponse(result.Content, questionIdAliasMap);
            }
            catch (OperationCanceledException)
            {
                throw;
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
            string operationName,
            CancellationToken cancellationToken = default)
        {
            var lastResult = AIOperationResult.InvalidOutput();

            for (var attempt = 1; attempt <= MaxAiAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

        private static int? TryGetInt32(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out var value)
                ? value
                : null;
        }

        private async Task LogPromptInputAsync(string promptType, string promptVersion, string? systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            if (!CanWritePromptFileLog())
            {
                return;
            }

            var formattedInput = FormatPromptInputForLog(systemPrompt, userPrompt);
            await WritePromptLogFileAsync(promptType, promptVersion, "INPUT", formattedInput, cancellationToken);
        }

        private async Task LogPromptOutputAsync(string promptType, string promptVersion, string output, CancellationToken cancellationToken = default)
        {
            if (!CanWritePromptFileLog())
            {
                return;
            }

            var formattedOutput = FormatPromptOutputForLog(output);
            await WritePromptLogFileAsync(promptType, promptVersion, "OUTPUT", formattedOutput, cancellationToken);
        }

        private async Task WritePromptLogFileAsync(string promptType, string promptVersion, string payloadType, string payload, CancellationToken cancellationToken = default)
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
                await File.AppendAllTextAsync(logPath, entry, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
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
                return JsonSerializer.Serialize(jsonObject, AIJsonDefaults.Indented);
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
                return JsonSerializer.Serialize(contentObject, AIJsonDefaults.Indented);
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
