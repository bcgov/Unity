using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Models;
using Unity.AI.Prompts;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime
{
    [ExposeServices(typeof(IAIService))]
    public class OpenAIRuntimeService : IAIService, ITransientDependency
    {
        private readonly ILogger<OpenAIRuntimeService> _logger;
        private readonly OpenAITransportService _openAITransportService;
        private readonly OpenAIConfigurationResolver _openAIConfigurationResolver;
        private readonly IAIPromptTemplateProvider _promptTemplateProvider;
        private readonly OpenAIPromptFileLogger _promptFileLogger;
        private const string ApplicationAnalysisPromptType = AIPromptTypes.ApplicationAnalysis;
        private const string AttachmentSummaryPromptType = AIPromptTypes.AttachmentSummary;
        private const string ApplicationScoringPromptType = AIPromptTypes.ApplicationScoring;
        private const int MaxAiAttempts = 3;

        public OpenAIRuntimeService(
            ILogger<OpenAIRuntimeService> logger,
            OpenAITransportService openAITransportService,
            OpenAIConfigurationResolver openAIConfigurationResolver,
            IAIPromptTemplateProvider promptTemplateProvider,
            OpenAIPromptFileLogger promptFileLogger)
        {
            _logger = logger;
            _openAITransportService = openAITransportService;
            _openAIConfigurationResolver = openAIConfigurationResolver;
            _promptTemplateProvider = promptTemplateProvider;
            _promptFileLogger = promptFileLogger;
        }

        public Task<bool> IsAvailableAsync()
        {
            return IsAvailableCoreAsync();
        }

        public async Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var settings = await _openAIConfigurationResolver.ResolveOperationSettingsAsync(ApplicationAnalysisPromptType, cancellationToken);
                var promptTemplate = await _promptTemplateProvider.GetRequiredPromptAsync(
                    ApplicationAnalysisPromptType,
                    request.PromptVersion ?? settings.PromptVersion,
                    cancellationToken);
                var promptVersion = promptTemplate.PromptVersion;
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
                var systemPrompt = promptTemplate.SystemPrompt;
                var applicationAnalysisContent = AIPromptTemplateRenderer.BuildApplicationAnalysisUserPrompt(
                    promptTemplate.UserPrompt,
                    schema,
                    data,
                    attachments,
                    promptTemplate.MetadataJson);

                await _promptFileLogger.LogPromptInputAsync(ApplicationAnalysisPromptType, promptVersion, systemPrompt, applicationAnalysisContent, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        applicationAnalysisContent,
                        systemPrompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.ValidateApplicationAnalysisJson,
                    "application analysis",
                    cancellationToken);

                await _promptFileLogger.LogPromptOutputAsync(ApplicationAnalysisPromptType, promptVersion, result.CaptureOutput, cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    var providerDetails = result.Response?.RawResponse;
                    if (string.IsNullOrWhiteSpace(providerDetails))
                    {
                        providerDetails = result.Response?.Content;
                    }

                    if (!string.IsNullOrWhiteSpace(providerDetails) && providerDetails.Length > 400)
                    {
                        providerDetails = providerDetails[..400];
                    }

                    _logger.LogError(
                        "Application analysis generation failed with outcome {Outcome} and failure category {FailureCategory}. HTTP status {HttpStatusCode}. Provider details: {ProviderDetails}",
                        result.Outcome,
                        result.FailureCategory,
                        result.Response?.HttpStatusCode?.ToString() ?? "n/a",
                        providerDetails ?? "n/a");

                    throw new UserFriendlyException("Application analysis generation failed.");
                }

                return OpenAIResponseParser.ParseApplicationAnalysisResponse(result.Content);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application analysis generation failed.");
                throw;
            }
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var fileName = request.FileName ?? string.Empty;
            var contentType = request.ContentType ?? "application/octet-stream";

            try
            {
                var settings = await _openAIConfigurationResolver.ResolveOperationSettingsAsync(AttachmentSummaryPromptType, cancellationToken);
                var promptTemplate = await _promptTemplateProvider.GetRequiredPromptAsync(
                    AttachmentSummaryPromptType,
                    request.PromptVersion ?? settings.PromptVersion,
                    cancellationToken);
                var promptVersion = promptTemplate.PromptVersion;
                var extractedText = request.ExtractedText;
                var prompt = promptTemplate.SystemPrompt;

                var attachmentText = string.IsNullOrWhiteSpace(extractedText) ? null : extractedText;
                var attachmentPayload = new[]
                {
                    new
                    {
                        name = fileName,
                        contentType,
                        text = attachmentText
                    }
                };
                var attachments = JsonSerializer.Serialize(attachmentPayload, AIJsonDefaults.Indented);
                var contentToAnalyze = AIPromptTemplateRenderer.BuildAttachmentSummaryBatchUserPrompt(
                    promptTemplate.UserPrompt,
                    attachments,
                    promptTemplate.MetadataJson);

                await _promptFileLogger.LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, prompt, contentToAnalyze, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        contentToAnalyze,
                        prompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.ValidateAttachmentSummaryText,
                    "attachment summary",
                    cancellationToken);
                await _promptFileLogger.LogPromptOutputAsync(AttachmentSummaryPromptType, promptVersion, result.CaptureOutput, cancellationToken);

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attachment summary generation failed for {FileName}.", fileName);
                return new AttachmentSummaryResponse
                {
                    Summary = $"AI analysis not available for this attachment ({fileName})."
                };
            }
        }

        public async Task<AttachmentSummaryBatchResponse> GenerateAttachmentSummaryBatchAsync(AttachmentSummaryBatchRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            try
            {
                if (request.Attachments is null || request.Attachments.Count == 0)
                {
                    return new AttachmentSummaryBatchResponse();
                }

                var settings = await _openAIConfigurationResolver.ResolveOperationSettingsAsync(AttachmentSummaryPromptType, cancellationToken);
                var promptTemplate = await _promptTemplateProvider.GetRequiredPromptAsync(
                    AttachmentSummaryPromptType,
                    request.PromptVersion ?? settings.PromptVersion,
                    cancellationToken);
                var promptVersion = promptTemplate.PromptVersion;

                var attachmentsPayload = request.Attachments.Select(attachment => new
                {
                    attachmentId = attachment.AttachmentId,
                    name = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName.Trim(),
                    contentType = attachment.ContentType ?? "application/octet-stream",
                    text = string.IsNullOrWhiteSpace(attachment.ExtractedText) ? null : attachment.ExtractedText
                });

                var attachments = JsonSerializer.Serialize(attachmentsPayload, AIJsonDefaults.Indented);
                var contentToAnalyze = AIPromptTemplateRenderer.BuildAttachmentSummaryBatchUserPrompt(
                    promptTemplate.UserPrompt,
                    attachments,
                    promptTemplate.MetadataJson);

                await _promptFileLogger.LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, promptTemplate.SystemPrompt, contentToAnalyze, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        contentToAnalyze,
                        promptTemplate.SystemPrompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.ValidateAttachmentSummaryBatchJson,
                    "attachment summary batch",
                    cancellationToken);
                await _promptFileLogger.LogPromptOutputAsync(AttachmentSummaryPromptType, promptVersion, result.CaptureOutput, cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new AttachmentSummaryBatchResponse();
                }

                return OpenAIResponseParser.ParseAttachmentSummaryBatchResponse(result.Content);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attachment summary batch generation failed.");
                return new AttachmentSummaryBatchResponse();
            }
        }

        public async Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            try
            {
                var settings = await _openAIConfigurationResolver.ResolveOperationSettingsAsync(ApplicationScoringPromptType, cancellationToken);
                var promptTemplate = await _promptTemplateProvider.GetRequiredPromptAsync(
                    ApplicationScoringPromptType,
                    request.PromptVersion ?? settings.PromptVersion,
                    cancellationToken);
                var promptVersion = promptTemplate.PromptVersion;
                var dataJson = JsonSerializer.Serialize(request.Data, AIJsonDefaults.Indented);
                var sectionJson = JsonSerializer.Serialize(request.SectionSchema, AIJsonDefaults.Indented);

                var attachmentSummaries = request.Attachments
                    .Select(a => $"{a.Name}: {a.Summary}")
                    .ToList();
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

                var applicationScoringContent = AIPromptTemplateRenderer.BuildApplicationScoringUserPrompt(
                    promptTemplate.UserPrompt,
                    dataJson,
                    attachments,
                    section,
                    response,
                    promptTemplate.MetadataJson);
                var systemPrompt = promptTemplate.SystemPrompt;

                await _promptFileLogger.LogPromptInputAsync(ApplicationScoringPromptType, promptVersion, systemPrompt, applicationScoringContent, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        applicationScoringContent,
                        systemPrompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    content => AIProviderPayloadValidator.ValidateApplicationScoringJson(content, section),
                    $"application scoring section {request.SectionName}",
                    cancellationToken);
                await _promptFileLogger.LogPromptOutputAsync(ApplicationScoringPromptType, promptVersion, result.CaptureOutput, cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new ApplicationScoringResponse();
                }

                return OpenAIResponseParser.ParseApplicationScoringResponse(result.Content, questionIdAliasMap);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application scoring generation failed for section {SectionName}.", request.SectionName);
                return new ApplicationScoringResponse();
            }
        }

        private async Task<AIOperationResult> GenerateWithRetryAsync(
            Func<Task<AIOperationResult>> operation,
            Func<string, AIResponseValidationResult> validator,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            var lastResult = AIOperationResult.InvalidOutput();

            for (var attempt = 1; attempt <= MaxAiAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lastResult = await operation();

                if (lastResult.Outcome == AIOperationOutcome.Success)
                {
                    var validationResult = validator(lastResult.Content);
                    if (validationResult.IsValid)
                    {
                        return lastResult;
                    }

                    lastResult = lastResult.WithOutcome(AIOperationOutcome.InvalidOutput, validationResult.FailureCategory);

                    _logger.LogWarning(
                        "AI {OperationName} attempt {Attempt}/{MaxAttempts} returned invalid response shape ({FailureCategory}): {Reason}; will retry if attempts remain",
                        operationName,
                        attempt,
                        MaxAiAttempts,
                        validationResult.FailureCategory,
                        validationResult.Reason ?? "No validation reason provided");
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
                            "AI {OperationName} attempt {Attempt}/{MaxAttempts} failed transiently ({FailureCategory}); retrying",
                            operationName,
                            attempt,
                            MaxAiAttempts,
                            lastResult.FailureCategory);
                    }
                    else if (lastResult.Outcome == AIOperationOutcome.InvalidOutput)
                    {
                        _logger.LogWarning(
                            "AI {OperationName} attempt {Attempt}/{MaxAttempts} returned invalid output ({FailureCategory}); retrying",
                            operationName,
                            attempt,
                            MaxAiAttempts,
                            lastResult.FailureCategory);
                    }
                }
            }

            _logger.LogWarning(
                "AI {OperationName} exhausted retries with outcome {Outcome} and failure category {FailureCategory}; HTTP status {HttpStatusCode}; model {Model}; returning last result",
                operationName,
                lastResult.Outcome,
                lastResult.FailureCategory,
                lastResult.Response.HttpStatusCode,
                lastResult.Response.Model);
            return lastResult;
        }

        private async Task<bool> IsAvailableCoreAsync()
        {
            try
            {
                await _openAIConfigurationResolver.ResolveApiKeyAsync();
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI is unavailable because the OpenAI configuration could not be resolved.");
                return false;
            }
        }

        private static string ResolveNarrativeContent(AIOperationResult result)
        {
            return result.Outcome switch
            {
                AIOperationOutcome.Success => result.Content,
                AIOperationOutcome.PermanentFailure => "AI service not available - service not configured.",
                AIOperationOutcome.TransientFailure => "AI request failed - service temporarily unavailable.",
                _ => "AI request failed - please try again later."
            };
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
