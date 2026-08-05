using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAITransportService(
    OpenAIChatClientFactory chatClientFactory,
    ILogger<OpenAITransportService> logger) : ITransientDependency
{
    private readonly OpenAIChatClientFactory _chatClientFactory = chatClientFactory;
    private readonly ILogger<OpenAITransportService> _logger = logger;

    public async Task<AIOperationResult> GenerateSummaryAsync(
        string content,
        string? systemPrompt,
        OpenAIOperationSettings settings,
        int maxTokens = 150,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.Equals(settings.ProviderName, "OpenAI", StringComparison.Ordinal))
            {
                _logger.LogWarning("Provider {ProviderName} is not supported by OpenAI transport.", settings.ProviderName);
                return AIOperationResult.PermanentFailure(new AIProviderResult($"Unsupported provider: {settings.ProviderName}"));
            }

            var resolvedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? "You are a professional grant analyst for the BC Government."
                : systemPrompt;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(resolvedSystemPrompt),
                new UserChatMessage(content ?? string.Empty)
            };

            var result = await CompleteChatWithTemperatureFallbackAsync(
                settings,
                messages,
                maxTokens,
                cancellationToken);

            var completion = result.Value;
            var rawResponse = result.GetRawResponse();
            var statusCode = rawResponse?.Status;
            var responseContent = rawResponse?.Content?.ToString() ?? string.Empty;
            var modelOutput = ExtractModelOutput(completion, responseContent);
            var providerResponse = BuildProviderResponseFromMetadata(
                modelOutput ?? string.Empty,
                responseContent,
                TryExtractProviderMetadata(responseContent),
                statusCode);

            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                LogEmptyModelOutput(completion, providerResponse, statusCode ?? 0);
            }

            return string.IsNullOrWhiteSpace(modelOutput)
                ? AIOperationResult.InvalidOutput(providerResponse)
                : AIOperationResult.Success(providerResponse);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI request configuration is invalid.");
            return AIOperationResult.PermanentFailure(new AIProviderResult(ex.Message));
        }
        catch (ClientResultException ex)
        {
            int? statusCode = ex.Status > 0 ? ex.Status : null;
            var responseContent = ex.GetRawResponse()?.Content?.ToString() ?? ex.Message;
            var providerResponse = BuildProviderResponseFromMetadata(
                string.Empty,
                responseContent,
                TryExtractProviderMetadata(responseContent),
                statusCode);

            _logger.LogError(
                ex,
                "OpenAI API request failed with status {StatusCode}. Response body length: {ResponseLength}.",
                statusCode,
                responseContent.Length);
            return statusCode.HasValue
                ? MapFailureOutcome((HttpStatusCode)statusCode.Value, providerResponse)
                : AIOperationResult.TransientFailure(providerResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary");
            return AIOperationResult.TransientFailure(new AIProviderResult(ex.Message));
        }
    }

    private async Task<ClientResult<ChatCompletion>> CompleteChatWithTemperatureFallbackAsync(
        OpenAIOperationSettings settings,
        IReadOnlyList<ChatMessage> messages,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _chatClientFactory.Create(settings).CompleteChatAsync(
                messages,
                BuildOptions(settings, maxTokens, includeTemperature: true),
                cancellationToken);
        }
        catch (ClientResultException ex)
        {
            var responseContent = ex.GetRawResponse()?.Content?.ToString() ?? ex.Message;
            if (!ShouldRetryWithoutTemperature(settings, ex.Status, responseContent))
            {
                throw;
            }

            _logger.LogWarning(
                ex,
                "Retrying OpenAI request without temperature after provider rejected the temperature parameter for profile {ProfileName}.",
                settings.ProfileName);

            return await _chatClientFactory.Create(settings).CompleteChatAsync(
                messages,
                BuildOptions(settings, maxTokens, includeTemperature: false),
                cancellationToken);
        }
    }

    private static ChatCompletionOptions BuildOptions(OpenAIOperationSettings settings, int maxTokens, bool includeTemperature)
    {
        var options = new ChatCompletionOptions();
        if (settings.MaxOutputTokenCountSupported)
        {
            options.MaxOutputTokenCount = maxTokens;
        }

        if (includeTemperature && settings.Temperature.HasValue)
        {
            options.Temperature = (float)settings.Temperature.Value;
        }

        return options;
    }

    private static bool ShouldRetryWithoutTemperature(OpenAIOperationSettings settings, int statusCode, string responseContent)
    {
        if (!settings.Temperature.HasValue || statusCode != 400 || string.IsNullOrWhiteSpace(responseContent))
        {
            return false;
        }

        var lowered = responseContent.ToLowerInvariant();
        return lowered.Contains("temperature")
            && (lowered.Contains("unsupported")
                || lowered.Contains("not supported")
                || lowered.Contains("not allowed")
                || lowered.Contains("invalid"));
    }

    private static AIOperationResult MapFailureOutcome(HttpStatusCode statusCode, AIProviderResult response)
    {
        var statusCodeValue = (int)statusCode;
        if (statusCode == HttpStatusCode.RequestTimeout || statusCode == (HttpStatusCode)429 || statusCodeValue >= 500)
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

    private void LogEmptyModelOutput(ChatCompletion completion, AIProviderResult response, int statusCode)
    {
        _logger.LogWarning(
            "AI model output was empty. StatusCode: {StatusCode}; FinishReason: {FinishReason}; ContentPartCount: {ContentPartCount}; PromptTokens: {PromptTokens}; CompletionTokens: {CompletionTokens}; TotalTokens: {TotalTokens}; ReasoningTokens: {ReasoningTokens}.",
            statusCode,
            response.FinishReason,
            completion.Content.Count,
            response.PromptTokens,
            response.CompletionTokens,
            response.TotalTokens,
            response.ReasoningTokens);
    }

    private static string? ExtractModelOutput(ChatCompletion completion, string? responseContent)
    {
        var contentParts = new List<string>();
        foreach (var part in completion.Content)
        {
            if (!string.IsNullOrWhiteSpace(part.Text))
            {
                contentParts.Add(part.Text!);
            }
        }

        if (contentParts.Count > 0)
        {
            return string.Concat(contentParts);
        }

        return TryExtractMessageContent(responseContent);
    }

    private static string? TryExtractMessageContent(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(responseContent);
            if (!TryGetFirstChoice(jsonDoc.RootElement, out var firstChoice)
                || !firstChoice.TryGetProperty("message", out var message)
                || !message.TryGetProperty("content", out var content))
            {
                return null;
            }

            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            if (content.ValueKind == JsonValueKind.Array)
            {
                return ExtractTextContentParts(content);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryGetFirstChoice(JsonElement root, out JsonElement firstChoice)
    {
        firstChoice = default;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            return false;
        }

        firstChoice = choices[0];
        return true;
    }

    private static string? ExtractTextContentParts(JsonElement content)
    {
        var parts = new List<string>();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.Object
                && part.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            {
                parts.Add(text.GetString() ?? string.Empty);
            }
        }

        return parts.Count > 0 ? string.Concat(parts) : null;
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

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number
            && prop.TryGetInt32(out var value)
            ? value
            : null;
    }
}
