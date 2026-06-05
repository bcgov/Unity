using Azure.AI.OpenAI;
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
    OpenAIConfigurationResolver configurationResolver,
    ILogger<OpenAITransportService> logger) : ITransientDependency
{
    private readonly OpenAIConfigurationResolver _configurationResolver = configurationResolver;
    private readonly ILogger<OpenAITransportService> _logger = logger;

    public async Task<AIOperationResult> GenerateSummaryAsync(
        string content,
        string? systemPrompt,
        int maxTokens = 150,
        double? temperature = null,
        string? operationName = null,
        string? promptVersion = null,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var providerName = _configurationResolver.ResolveProviderName(operationName);
            if (!string.Equals(providerName, "OpenAI", StringComparison.Ordinal))
            {
                _logger.LogWarning("Provider {ProviderName} is not supported by OpenAI transport.", providerName);
                return AIOperationResult.PermanentFailure(new AIProviderResult($"Unsupported provider: {providerName}"));
            }

            var apiKey = _configurationResolver.ResolveApiKey(operationName);
            var resolvedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? "You are a professional grant analyst for the BC Government."
                : systemPrompt;

            var clientOptions = new AzureOpenAIClientOptions();
            var resolvedApiVersion = _configurationResolver.ResolveApiVersion(operationName);
            // Note: Azure SDK uses its default API version. Configured version is parsed for documentation/future use.
            if (!string.IsNullOrEmpty(resolvedApiVersion))
            {
                _logger.LogDebug("Profile specifies API version {ApiVersion}; SDK will use its default", resolvedApiVersion);
            }

            var client = new AzureOpenAIClient(
                _configurationResolver.ResolveEndpoint(operationName),
                new ApiKeyCredential(apiKey),
                clientOptions)
                .GetChatClient(_configurationResolver.ResolveDeploymentName(operationName));

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = maxTokens
            };

            var profileTemperature = _configurationResolver.ResolveConfiguredTemperature(operationName);
            var resolvedTemperature = temperature ?? profileTemperature;
            if (resolvedTemperature.HasValue)
            {
                options.Temperature = (float)resolvedTemperature.Value;
            }

            var result = await client.CompleteChatAsync(
                [
                    new SystemChatMessage(resolvedSystemPrompt),
                    new UserChatMessage(content ?? string.Empty)
                ],
                options,
                cancellationToken);

            var completion = result.Value;
            var rawResponse = result.GetRawResponse();
            var responseContent = rawResponse.Content.ToString();
            var modelOutput = completion.Content.Count > 0 ? completion.Content[0].Text : null;
            var providerResponse = BuildProviderResponseFromMetadata(
                modelOutput ?? string.Empty,
                responseContent,
                TryExtractProviderMetadata(responseContent),
                rawResponse.Status);

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

            _logger.LogError(ex, "OpenAI API request failed: {StatusCode} - {Content}", statusCode, responseContent);
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
