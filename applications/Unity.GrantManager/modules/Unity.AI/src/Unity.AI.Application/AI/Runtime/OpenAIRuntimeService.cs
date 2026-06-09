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
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime
{
    [ExposeServices(typeof(IAIService))]
    public class OpenAIRuntimeService : IAIService, ITransientDependency
    {
        private readonly ILogger<OpenAIRuntimeService> _logger;
        private readonly OpenAITransportService _openAITransportService;
        private readonly OpenAIConfigurationResolver _openAIConfigurationResolver;
        private readonly OpenAIPromptFileLogger _promptFileLogger;
        private const string ApplicationAnalysisPromptType = AIPromptTypes.ApplicationAnalysis;
        private const string AttachmentSummaryPromptType = AIPromptTypes.AttachmentSummary;
        private const string ApplicationScoringPromptType = AIPromptTypes.ApplicationScoring;
        private const int MaxAiAttempts = 3;



        public OpenAIRuntimeService(
            ILogger<OpenAIRuntimeService> logger,
            OpenAITransportService openAITransportService,
            OpenAIConfigurationResolver openAIConfigurationResolver,
            OpenAIPromptFileLogger promptFileLogger)
        {
            _logger = logger;
            _openAITransportService = openAITransportService;
            _openAIConfigurationResolver = openAIConfigurationResolver;
            _promptFileLogger = promptFileLogger;
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
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var settings = _openAIConfigurationResolver.ResolveOperationSettings(ApplicationAnalysisPromptType);
                var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? settings.PromptVersion);
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

                await _promptFileLogger.LogPromptInputAsync(ApplicationAnalysisPromptType, promptVersion, systemPrompt, applicationAnalysisContent, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        applicationAnalysisContent,
                        systemPrompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.IsValidApplicationAnalysisJson,
                    "application analysis",
                    cancellationToken);

                await _promptFileLogger.LogPromptOutputAsync(ApplicationAnalysisPromptType, promptVersion, result.CaptureOutput, cancellationToken);

                if (result.Outcome != AIOperationOutcome.Success)
                {
                    return new ApplicationAnalysisResponse();
                }

                return OpenAIResponseParser.ParseApplicationAnalysisResponse(result.Content);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application analysis.");
                return new ApplicationAnalysisResponse();
            }
        }

        public async Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var fileName = request.FileName ?? string.Empty;
            var contentType = request.ContentType ?? "application/octet-stream";
            var settings = _openAIConfigurationResolver.ResolveOperationSettings(AttachmentSummaryPromptType);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? settings.PromptVersion);

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

                await _promptFileLogger.LogPromptInputAsync(AttachmentSummaryPromptType, promptVersion, prompt, contentToAnalyze, cancellationToken);
                var result = await GenerateWithRetryAsync(
                    () => _openAITransportService.GenerateSummaryAsync(
                        contentToAnalyze,
                        prompt,
                        settings,
                        settings.CompletionTokens,
                        cancellationToken: cancellationToken),
                    AIProviderPayloadValidator.IsValidAttachmentSummaryText,
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
            var settings = _openAIConfigurationResolver.ResolveOperationSettings(ApplicationScoringPromptType);
            var promptVersion = OpenAIPromptRenderer.ResolvePromptVersion(request.PromptVersion ?? settings.PromptVersion);
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

                await _promptFileLogger.LogPromptInputAsync(ApplicationScoringPromptType, promptVersion, systemPrompt, applicationScoringContent, cancellationToken);
                var result = await GenerateWithRetryAsync(
                () => _openAITransportService.GenerateSummaryAsync(
                    applicationScoringContent,
                    systemPrompt,
                    settings,
                    settings.CompletionTokens,
                    cancellationToken: cancellationToken),
                    content => AIProviderPayloadValidator.IsValidApplicationScoringJson(content, section),
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
