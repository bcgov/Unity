using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.AI.Extraction;
using Unity.GrantManager.AI.Models;
using Unity.GrantManager.AI.Prompts;
using Unity.GrantManager.AI.Requests;
using Unity.GrantManager.AI.Responses;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.Runtime
{
    [ExposeServices(typeof(IAIService))]
    public class OpenAIRuntimeService : IAIService, ITransientDependency
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIRuntimeService> _logger;
        private readonly ITextExtractionService _textExtractionService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IHostEnvironment _hostEnvironment;
        private const string ApplicationAnalysisPromptType = AIPromptTypes.ApplicationAnalysis;
        private const string AttachmentSummaryPromptType = AIPromptTypes.AttachmentSummary;
        private const string ApplicationScoringPromptType = AIPromptTypes.ApplicationScoring;
        private const string PromptVersionV0 = "v0";
        private const string PromptVersionV1 = "v1";
        private static readonly string PromptTemplatesFolder = Path.Combine("AI", "Prompts", "Versions");
        private const string ApplicationAnalysisSystemTemplateName = "application-analysis.system";
        private const string ApplicationAnalysisUserTemplateName = "application-analysis.user";
        private const string AttachmentSummarySystemTemplateName = "attachment-summary.system";
        private const string AttachmentSummaryUserTemplateName = "attachment-summary.user";
        private const string ApplicationScoringSystemTemplateName = "application-scoring.system";
        private const string ApplicationScoringUserTemplateName = "application-scoring.user";
        private const string AIServiceNotConfiguredMessage = "AI service not available - service not configured.";
        private const string AIServiceTemporarilyUnavailableMessage = "AI request failed - service temporarily unavailable.";
        private const string AIRequestFailedRetryMessage = "AI request failed - please try again later.";
        private const int MaxAiAttempts = 3;
        private const string DefaultMaxTokensParameterName = "max_completion_tokens";
        private const string LegacyMaxTokensParameterName = "max_tokens";
        private const string DefaultProviderName = "OpenAI";
        private const int DefaultCompletionTokens = 2000;
        private const int DefaultAttachmentSummaryCompletionTokens = 2000;
        private const int DefaultApplicationAnalysisCompletionTokens = 4000;
        private const int DefaultApplicationScoringCompletionTokens = 8000;

        private int AttachmentSummaryCompletionTokens => ResolveCompletionTokens(AttachmentSummaryPromptType, DefaultAttachmentSummaryCompletionTokens);
        private int ApplicationAnalysisCompletionTokens => ResolveCompletionTokens(ApplicationAnalysisPromptType, DefaultApplicationAnalysisCompletionTokens);
        private int ApplicationScoringCompletionTokens => ResolveCompletionTokens(ApplicationScoringPromptType, DefaultApplicationScoringCompletionTokens);
        private readonly string MissingApiKeyMessage = "OpenAI API key is not configured";

        // Optional local debugging sink for prompt payload logs to a local file.
        // Not intended for deployed/shared environments.
        private bool IsPromptFileLoggingEnabled => _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
        private const string PromptLogDirectoryName = "logs";
        private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

        private static readonly JsonSerializerOptions JsonLogOptions = new() { WriteIndented = true };

        private static readonly Dictionary<string, string> PromptProfiles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PromptVersionV0] = PromptVersionV0,
                [PromptVersionV1] = PromptVersionV1
            };
        private static readonly ConcurrentDictionary<string, string> PromptTemplateCache = new(StringComparer.OrdinalIgnoreCase);

        public OpenAIRuntimeService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIRuntimeService> logger,
            ITextExtractionService textExtractionService,
            ICurrentTenant currentTenant,
            IHostEnvironment hostEnvironment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _textExtractionService = textExtractionService;
            _currentTenant = currentTenant;
            _hostEnvironment = hostEnvironment;
        }

        public Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrEmpty(ResolveApiKey()))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public async Task<AICompletionResponse> GenerateCompletionAsync(AICompletionRequest request)
        {
            var result = await GenerateWithRetryAsync(
                () => GenerateSummaryAsync(
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
            var promptVersion = ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(ApplicationAnalysisPromptType));
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
            var systemPrompt = BuildApplicationAnalysisSystemPrompt(promptVersion);
            var applicationAnalysisContent = BuildApplicationAnalysisUserPrompt(
                promptVersion,
                schema,
                data,
                attachments);
            await LogPromptInputAsync(ApplicationAnalysisPromptType, promptVersion, systemPrompt, applicationAnalysisContent);
            var result = await GenerateWithRetryAsync(
                () => GenerateSummaryAsync(
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

            return ParseApplicationAnalysisResponse(AddIdsToAnalysisItems(result.Content));
        }

        private async Task<AIOperationResult> GenerateSummaryAsync(
            string content,
            string? systemPrompt,
            int maxTokens = 150,
            double? temperature = null,
            string? operationName = null,
            string? promptVersion = null,
            string? fileName = null)
        {
            var providerName = ResolveProviderName(operationName);
            if (!string.Equals(providerName, DefaultProviderName, StringComparison.Ordinal))
            {
                _logger.LogWarning("Provider {ProviderName} is not supported by OpenAIRuntimeService.", providerName);
                return AIOperationResult.PermanentFailure(new AIProviderResult($"Unsupported provider: {providerName}"));
            }

            var apiKey = ResolveApiKey(operationName);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Error: {Message}", MissingApiKeyMessage);
                return AIOperationResult.PermanentFailure(new AIProviderResult(MissingApiKeyMessage));
            }

            _logger.LogDebug("Calling OpenAI chat completions. PromptLength: {PromptLength}, MaxTokens: {MaxTokens}", content?.Length ?? 0, maxTokens);

            try
            {
                var resolvedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                    ? "You are a professional grant analyst for the BC Government."
                    : systemPrompt;
                var userPrompt = content ?? string.Empty;

                var requestBody = new
                {
                    messages = new[]
                    {
                       new { role = "system", content = resolvedSystemPrompt },
                       new { role = "user", content = userPrompt }
                   }
                };

                var requestPayload = new Dictionary<string, object?>
                {
                    ["messages"] = requestBody.messages,
                    [ResolveMaxTokensParameterNameForOperation(operationName)] = maxTokens
                };

                var resolvedTemperature = temperature ?? ResolveConfiguredTemperature(operationName);
                if (resolvedTemperature.HasValue)
                {
                    requestPayload["temperature"] = resolvedTemperature.Value;
                }

                var json = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);

                var response = await _httpClient.PostAsync(ResolveApiUrl(operationName), httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                var metadata = TryExtractProviderMetadata(responseContent);
                var providerResponse = BuildProviderResponseFromMetadata(
                    string.Empty,
                    responseContent,
                    metadata,
                    (int)response.StatusCode);

                _logger.LogDebug(
                    "OpenAI chat completions response received. StatusCode: {StatusCode}, ResponseLength: {ResponseLength}",
                    response.StatusCode,
                    responseContent?.Length ?? 0);
                LogProviderMetadata(operationName, promptVersion, fileName, providerResponse, response.IsSuccessStatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return MapFailureOutcome(response.StatusCode, providerResponse);
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return AIOperationResult.InvalidOutput(providerResponse);
                }

                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseContent);
                    var choices = jsonDoc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        var modelOutput = message.GetProperty("content").GetString();
                        return string.IsNullOrWhiteSpace(modelOutput)
                            ? AIOperationResult.InvalidOutput(providerResponse)
                            : AIOperationResult.Success(BuildProviderResponseFromMetadata(
                                modelOutput,
                                responseContent,
                                metadata,
                                (int)response.StatusCode));
                    }

                    return AIOperationResult.InvalidOutput(providerResponse);
                }
                catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
                {
                    _logger.LogWarning(ex, "AI response payload had an invalid output shape");
                    return AIOperationResult.InvalidOutput(providerResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary");
                return AIOperationResult.TransientFailure(new AIProviderResult(ex.Message));
            }
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var fileName = request.FileName ?? string.Empty;
            var fileContent = request.FileContent ?? Array.Empty<byte>();
            var contentType = request.ContentType ?? "application/octet-stream";
            var promptVersion = ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(AttachmentSummaryPromptType));

            try
            {
                var extractedText = await _textExtractionService.ExtractTextAsync(fileName, fileContent, contentType);
                var prompt = BuildAttachmentSummarySystemPrompt(promptVersion);

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
                var contentToAnalyze = BuildAttachmentSummaryUserPrompt(promptVersion, attachment);

                await LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, prompt, contentToAnalyze);
                var result = await GenerateWithRetryAsync(
                () => GenerateSummaryAsync(
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

                        if (outputPropertyName == AIJsonKeys.Errors ||
                            outputPropertyName == AIJsonKeys.Warnings ||
                            outputPropertyName == AIJsonKeys.Summaries ||
                            outputPropertyName == AIJsonKeys.NextSteps)
                        {
                            writer.WritePropertyName(outputPropertyName);
                            writer.WriteStartArray();

                            foreach (var item in property.Value.EnumerateArray())
                            {
                                writer.WriteStartObject();

                                // Add unique ID first
                                writer.WriteString("id", Guid.NewGuid().ToString());
                                writer.WriteBoolean(AIJsonKeys.Hidden, false);

                                // Copy existing properties
                                foreach (var itemProperty in item.EnumerateObject())
                                {
                                    if (itemProperty.NameEquals(AIJsonKeys.Id) || itemProperty.NameEquals(AIJsonKeys.Hidden))
                                    {
                                        continue;
                                    }

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

        public async Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var promptVersion = ResolvePromptVersion(request.PromptVersion ?? ResolvePromptVersionSetting(ApplicationScoringPromptType));
            var dataJson = JsonSerializer.Serialize(request.Data, JsonLogOptions);
            var sectionJson = JsonSerializer.Serialize(request.SectionSchema, JsonLogOptions);

            var attachmentSummaries = request.Attachments
                .Select(a => $"{a.Name}: {a.Summary}")
                .ToList();
            if (string.IsNullOrEmpty(ResolveApiKey(ApplicationScoringPromptType)))
            {
                _logger.LogWarning("{Message}", MissingApiKeyMessage);
                return new ApplicationScoringResponse();
            }

            try
            {
                var attachments = attachmentSummaries.Count > 0
                    ? string.Join("\n- ", attachmentSummaries.Select((summary, index) => $"Attachment {index + 1}: {summary}"))
                    : "No attachments provided.";

                var section = BuildAliasedApplicationScoringSection(request.SectionName, sectionJson, out var questionIdAliasMap);
                var response = BuildApplicationScoringResponseTemplate(section);
                if (response == "{}")
                {
                    _logger.LogWarning(
                        "Skipping AI application scoring for section {SectionName} because response template could not be built from section schema.",
                        request.SectionName);
                    return new ApplicationScoringResponse();
                }

                var applicationScoringContent = BuildApplicationScoringUserPrompt(
                    promptVersion,
                    dataJson,
                    attachments,
                    section,
                    response);
                var systemPrompt = BuildApplicationScoringSystemPrompt(promptVersion);

                await LogPromptInputAsync(ApplicationScoringPromptType, promptVersion, systemPrompt, applicationScoringContent);
                var result = await GenerateWithRetryAsync(
                () => GenerateSummaryAsync(
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

                return ParseApplicationScoringResponse(result.Content, questionIdAliasMap);
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

        private static AIOperationResult MapFailureOutcome(HttpStatusCode statusCode, AIProviderResult response)
        {
            var statusCodeValue = (int)statusCode;

            if (statusCode == HttpStatusCode.RequestTimeout
                || statusCode == (HttpStatusCode)429
                || statusCodeValue >= 500)
            {
                return AIOperationResult.TransientFailure(response);
            }

            return AIOperationResult.PermanentFailure(response);
        }

        private static AIProviderResult BuildProviderResponseFromMetadata(
            string content,
            string? rawResponse,
            AIProviderResponseMetadata? metadata,
            int? httpStatusCode = null)
        {
            return new AIProviderResult(
                content,
                rawResponse ?? string.Empty,
                metadata?.Model,
                metadata?.FinishReason,
                httpStatusCode,
                metadata?.PromptTokens,
                metadata?.CompletionTokens,
                metadata?.TotalTokens,
                metadata?.ReasoningTokens);
        }

        private static AIProviderResponseMetadata? TryExtractProviderMetadata(string? responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;
                var model = root.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String
                    ? modelProp.GetString()
                    : null;

                string? finishReason = null;
                if (root.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("finish_reason", out var finishReasonProp) && finishReasonProp.ValueKind == JsonValueKind.String)
                    {
                        finishReason = finishReasonProp.GetString();
                    }
                }

                int? promptTokens = null;
                int? completionTokens = null;
                int? totalTokens = null;
                int? reasoningTokens = null;
                if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                {
                    promptTokens = TryGetInt32(usage, "prompt_tokens");
                    completionTokens = TryGetInt32(usage, "completion_tokens");
                    totalTokens = TryGetInt32(usage, "total_tokens");

                    if (usage.TryGetProperty("completion_tokens_details", out var completionTokenDetails)
                        && completionTokenDetails.ValueKind == JsonValueKind.Object)
                    {
                        reasoningTokens = TryGetInt32(completionTokenDetails, "reasoning_tokens");
                    }
                }

                return new AIProviderResponseMetadata(model, finishReason, promptTokens, completionTokens, totalTokens, reasoningTokens);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void LogProviderMetadata(
            string? operationName,
            string? promptVersion,
            string? fileName,
            AIProviderResult response,
            bool success)
        {
            if (string.IsNullOrWhiteSpace(response.Model)
                && string.IsNullOrWhiteSpace(response.FinishReason)
                && response.HttpStatusCode == null
                && response.PromptTokens == null
                && response.CompletionTokens == null
                && response.TotalTokens == null
                && response.ReasoningTokens == null)
            {
                return;
            }

            if (response.PromptTokens != null || response.CompletionTokens != null || response.TotalTokens != null)
            {
                _logger.LogInformation(
                    "AI token usage. OperationName={OperationName}, InputTokens={InputTokens}, CompletionTokens={CompletionTokens}, TotalTokens={TotalTokens}, Environment={Environment}, TenantId={TenantId}, Status={Status}, PromptVersion={PromptVersion}, Model={Model}, HttpStatusCode={HttpStatusCode}, FileName={FileName}",
                    operationName ?? "completion",
                    response.PromptTokens,
                    response.CompletionTokens,
                    response.TotalTokens,
                    _hostEnvironment.EnvironmentName,
                    _currentTenant.Id,
                    success ? "success" : "failed",
                    promptVersion,
                    response.Model,
                    response.HttpStatusCode,
                    fileName);
            }

            _logger.LogDebug(
                "AI provider response metadata for {OperationName}: Model={Model}, FinishReason={FinishReason}, HttpStatusCode={HttpStatusCode}, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, TotalTokens={TotalTokens}, ReasoningTokens={ReasoningTokens}",
                operationName ?? "completion",
                response.Model,
                response.FinishReason,
                response.HttpStatusCode,
                response.PromptTokens,
                response.CompletionTokens,
                response.TotalTokens,
                response.ReasoningTokens);
        }

        private static int? TryGetInt32(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out var value)
                ? value
                : null;
        }

        private static string ResolveMaxTokensParameterName(string? configuredParameterName)
        {
            if (string.Equals(configuredParameterName, LegacyMaxTokensParameterName, StringComparison.Ordinal))
            {
                return LegacyMaxTokensParameterName;
            }

            return DefaultMaxTokensParameterName;
        }

        private int ResolveCompletionTokens(string operationName, int defaultValue)
        {
            var configuredValue = _configuration.GetValue<int?>($"Azure:Operations:{operationName}:MaxCompletionTokens");
            if (configuredValue is > 0)
            {
                return configuredValue.Value;
            }

            var defaultConfiguredValue = _configuration.GetValue<int?>("Azure:Operations:Defaults:MaxCompletionTokens");
            return defaultConfiguredValue is > 0 ? defaultConfiguredValue.Value : defaultValue;
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

        private string ResolveProviderName(string? operationName = null)
        {
            if (!string.IsNullOrWhiteSpace(operationName))
            {
                var configuredProvider = _configuration[$"Azure:Operations:{operationName}:Provider"];
                if (!string.IsNullOrWhiteSpace(configuredProvider))
                {
                    return configuredProvider.Trim();
                }
            }

            var defaultProvider = _configuration["Azure:Operations:Defaults:Provider"];
            return string.IsNullOrWhiteSpace(defaultProvider) ? DefaultProviderName : defaultProvider.Trim();
        }

        private string? ResolveApiKey(string? operationName = null)
        {
            var providerName = ResolveProviderName(operationName);
            return _configuration[$"Azure:{providerName}:ApiKey"];
        }

        private string ResolveMaxTokensParameterNameForOperation(string? operationName = null)
        {
            var providerName = ResolveProviderName(operationName);
            var profileName = ResolveProfileName(operationName);
            var profileParameterName = ResolveProfileSetting(providerName, profileName, "MaxTokensParameter");
            return ResolveMaxTokensParameterName(profileParameterName);
        }

        private double? ResolveConfiguredTemperature(string? operationName = null)
        {
            var providerName = ResolveProviderName(operationName);
            var profileName = ResolveProfileName(operationName);
            var profileTemperature = ResolveProfileSetting(providerName, profileName, "Temperature");
            if (profileTemperature != null
                && double.TryParse(profileTemperature, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedTemperature))
            {
                return parsedTemperature;
            }

            return null;
        }

        private string ResolveApiUrl(string? operationName)
        {
            var providerName = ResolveProviderName(operationName);
            var profileName = ResolveProfileName(operationName);
            var profileApiUrl = ResolveProfileSetting(providerName, profileName, "ApiUrl");
            var legacyOpenAiApiUrl = _configuration["Azure:OpenAI:ApiUrl"];

            if (!string.IsNullOrWhiteSpace(profileApiUrl))
            {
                return profileApiUrl;
            }

            if (!string.IsNullOrWhiteSpace(legacyOpenAiApiUrl))
            {
                return legacyOpenAiApiUrl;
            }

            throw new InvalidOperationException($"AI API URL is not configured for provider '{providerName}'.");
        }

        private string? ResolveProfileName(string? operationName)
        {
            if (!string.IsNullOrWhiteSpace(operationName))
            {
                var operationProfile = _configuration[$"Azure:Operations:{operationName}:Profile"];
                if (!string.IsNullOrWhiteSpace(operationProfile))
                {
                    return operationProfile.Trim();
                }
            }

            var defaultProfile = _configuration["Azure:Operations:Defaults:Profile"];
            return string.IsNullOrWhiteSpace(defaultProfile) ? null : defaultProfile.Trim();
        }

        private string? ResolveProfileSetting(string providerName, string? profileName, string settingName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return null;
            }

            var profileSetting = _configuration[$"Azure:{providerName}:Profiles:{profileName}:{settingName}"];
            return string.IsNullOrWhiteSpace(profileSetting) ? null : profileSetting;
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

            if (root.TryGetProperty(AIJsonKeys.NextSteps, out var nextSteps) && nextSteps.ValueKind == JsonValueKind.Array)
            {
                response.NextSteps = ParseFindings(nextSteps);
            }

            if (root.TryGetProperty(AIJsonKeys.Recommendation, out var recommendation) && recommendation.ValueKind == JsonValueKind.Object)
            {
                response.Recommendation = ParseRecommendation(recommendation);
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
                var hidden = item.TryGetProperty(AIJsonKeys.Hidden, out var hiddenProp) &&
                    (hiddenProp.ValueKind == JsonValueKind.True || hiddenProp.ValueKind == JsonValueKind.False) &&
                    hiddenProp.GetBoolean();
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
                    Hidden = hidden,
                    Title = title,
                    Detail = detail
                });
            }

            return findings;
        }

        private static ApplicationAnalysisRecommendation? ParseRecommendation(JsonElement recommendation)
        {
            string? decision = null;
            if (recommendation.TryGetProperty(AIJsonKeys.Decision, out var decisionProp) &&
                decisionProp.ValueKind == JsonValueKind.String)
            {
                decision = decisionProp.GetString();
            }

            string? rationale = null;
            if (recommendation.TryGetProperty(AIJsonKeys.Rationale, out var rationaleProp) &&
                rationaleProp.ValueKind == JsonValueKind.String)
            {
                rationale = rationaleProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(decision) && string.IsNullOrWhiteSpace(rationale))
            {
                return null;
            }

            return new ApplicationAnalysisRecommendation
            {
                Decision = decision,
                Rationale = rationale
            };
        }

        private static ApplicationScoringResponse ParseApplicationScoringResponse(
            string raw,
            IReadOnlyDictionary<string, string>? questionIdAliasMap = null)
        {
            var response = new ApplicationScoringResponse();
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

                var questionId = questionIdAliasMap != null &&
                                 questionIdAliasMap.TryGetValue(property.Name, out var originalQuestionId)
                    ? originalQuestionId
                    : property.Name;

                response.Answers[questionId] = new ApplicationScoringAnswer
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

        private static string BuildApplicationScoringResponseTemplate(string sectionPayloadJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(sectionPayloadJson);
                if (!doc.RootElement.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
                {
                    return "{}";
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
                    return "{}";
                }

                return JsonSerializer.Serialize(template, JsonLogOptions);
            }
            catch (JsonException)
            {
                return "{}";
            }
        }

        private static string BuildAliasedApplicationScoringSection(
            string? sectionName,
            string sectionJson,
            out IReadOnlyDictionary<string, string> questionIdAliasMap)
        {
            questionIdAliasMap = new Dictionary<string, string>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(sectionJson))
            {
                return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, JsonLogOptions);
            }

            try
            {
                using var sectionDoc = JsonDocument.Parse(sectionJson);
                if (sectionDoc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return JsonSerializer.Serialize(new { name = sectionName, questions = sectionDoc.RootElement.Clone() }, JsonLogOptions);
                }

                var aliasedQuestions = new List<Dictionary<string, object?>>();
                var aliasMap = new Dictionary<string, string>(StringComparer.Ordinal);
                var index = 1;

                foreach (var question in sectionDoc.RootElement.EnumerateArray())
                {
                    if (question.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var aliasedQuestion = new Dictionary<string, object?>(StringComparer.Ordinal);
                    string? questionAlias = null;

                    foreach (var property in question.EnumerateObject())
                    {
                        if (property.NameEquals("id") && property.Value.ValueKind == JsonValueKind.String)
                        {
                            var originalQuestionId = property.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(originalQuestionId))
                            {
                                questionAlias = $"q{index++}";
                                aliasMap[questionAlias] = originalQuestionId;
                                aliasedQuestion[property.Name] = questionAlias;
                                continue;
                            }
                        }

                        aliasedQuestion[property.Name] = property.Value.Clone();
                    }

                    if (!string.IsNullOrWhiteSpace(questionAlias))
                    {
                        aliasedQuestions.Add(aliasedQuestion);
                    }
                }

                questionIdAliasMap = aliasMap;
                return JsonSerializer.Serialize(new { name = sectionName, questions = aliasedQuestions }, JsonLogOptions);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, JsonLogOptions);
            }
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

        private static string ResolvePromptVersion(string? version)
        {
            if (!string.IsNullOrWhiteSpace(version) &&
                PromptProfiles.TryGetValue(version.Trim(), out var selectedVersion))
            {
                return selectedVersion;
            }

            return PromptVersionV1;
        }

        private static string BuildApplicationAnalysisSystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, ApplicationAnalysisSystemTemplateName);
        }

        private static string BuildApplicationAnalysisUserPrompt(
            string version,
            string schema,
            string data,
            string attachments)
        {
            var replacements = new Dictionary<string, string>
            {
                ["SCHEMA"] = schema,
                ["DATA"] = data,
                ["ATTACHMENTS"] = attachments
            };

            return RenderPromptTemplate(version, ApplicationAnalysisUserTemplateName, replacements);
        }

        private static string BuildAttachmentSummarySystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, AttachmentSummarySystemTemplateName);
        }

        private static string BuildAttachmentSummaryUserPrompt(string version, string attachment)
        {
            return RenderPromptTemplate(version, AttachmentSummaryUserTemplateName, new Dictionary<string, string>
            {
                ["ATTACHMENT"] = attachment
            });
        }

        private static string BuildApplicationScoringSystemPrompt(string version)
        {
            return GetRequiredPromptTemplate(version, ApplicationScoringSystemTemplateName);
        }

        private static string BuildApplicationScoringUserPrompt(
            string version,
            string data,
            string attachments,
            string section,
            string response)
        {
            return RenderPromptTemplate(version, ApplicationScoringUserTemplateName, new Dictionary<string, string>
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

            var loaded = PromptTemplateCache.GetOrAdd(cacheKey, _ => File.ReadAllText(path));
            if (string.IsNullOrWhiteSpace(loaded))
            {
                return false;
            }

            template = loaded;
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
                        resolutionStack).TrimEnd();
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

            if (string.Equals(placeholderName, "RESPONSE", StringComparison.Ordinal))
            {
                var outputCandidate = $"{baseTemplateName}.output";
                if (TryGetPromptTemplate(version, outputCandidate, out _))
                {
                    return outputCandidate;
                }
            }

            if (TryResolveCommonTemplateName(placeholderName, out var commonTemplateName) &&
                TryGetPromptTemplate(version, commonTemplateName, out _))
            {
                return commonTemplateName;
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

        private static HashSet<string> GetTemplatePlaceholders(string template)
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
