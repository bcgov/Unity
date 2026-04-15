using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Models;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

internal class OpenAITransportService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAITransportService> logger) : IOpenAITransportService, ITransientDependency
{
    private const string DefaultMaxTokensParameterName = "max_completion_tokens";
    private const string LegacyMaxTokensParameterName = "max_tokens";
    private const string DefaultProviderName = "OpenAI";
    private const string OpenAiApiKeyEnvironmentVariableName = "AZURE_OPENAI_API_KEY";
    private const string OpenAiEndpointEnvironmentVariableName = "AZURE_OPENAI_ENDPOINT";

    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenAITransportService> _logger = logger;

    public async Task<AIOperationResult> GenerateSummaryAsync(
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
            _logger.LogWarning("Provider {ProviderName} is not supported by OpenAI transport.", providerName);
            return AIOperationResult.PermanentFailure(new AIProviderResult($"Unsupported provider: {providerName}"));
        }

        var apiKey = ResolveApiKey(operationName);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Error: OpenAI API key is not configured");
            return AIOperationResult.PermanentFailure(new AIProviderResult("OpenAI API key is not configured"));
        }

        try
        {
            var resolvedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? "You are a professional grant analyst for the BC Government."
                : systemPrompt;

            var requestPayload = new Dictionary<string, object?>
            {
                ["messages"] = new[]
                {
                    new { role = "system", content = resolvedSystemPrompt },
                    new { role = "user", content = content ?? string.Empty }
                },
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

    private string ResolveApiKey(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        if (string.Equals(providerName, DefaultProviderName, StringComparison.Ordinal))
        {
            var injectedApiKey = _configuration[OpenAiApiKeyEnvironmentVariableName];
            if (!string.IsNullOrWhiteSpace(injectedApiKey))
            {
                return injectedApiKey;
            }
        }

        return _configuration[$"Azure:{providerName}:ApiKey"] ?? string.Empty;
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
            && double.TryParse(profileTemperature, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTemperature))
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
        var injectedEndpoint = ResolveInjectedEndpoint(providerName);
        var legacyOpenAiApiUrl = _configuration["Azure:OpenAI:ApiUrl"];

        if (!string.IsNullOrWhiteSpace(injectedEndpoint) && !string.IsNullOrWhiteSpace(profileApiUrl))
        {
            return CombineEndpointAndPath(injectedEndpoint, profileApiUrl);
        }

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

    private string? ResolveInjectedEndpoint(string providerName)
    {
        if (!string.Equals(providerName, DefaultProviderName, StringComparison.Ordinal))
        {
            return _configuration[$"Azure:{providerName}:Endpoint"];
        }

        var injectedEndpoint = _configuration[OpenAiEndpointEnvironmentVariableName];
        if (!string.IsNullOrWhiteSpace(injectedEndpoint))
        {
            return injectedEndpoint;
        }

        return _configuration["Azure:OpenAI:Endpoint"];
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

    private static string ResolveMaxTokensParameterName(string? configuredParameterName)
    {
        if (string.Equals(configuredParameterName, LegacyMaxTokensParameterName, StringComparison.Ordinal))
        {
            return LegacyMaxTokensParameterName;
        }

        return DefaultMaxTokensParameterName;
    }

    private static string CombineEndpointAndPath(string endpoint, string profilePath)
    {
        const char UrlPathSeparator = '/';

        if (Uri.TryCreate(profilePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var trimmedEndpoint = endpoint.Trim().TrimEnd(UrlPathSeparator);
        var trimmedPath = profilePath.Trim();
        if (!trimmedPath.StartsWith(UrlPathSeparator))
        {
            trimmedPath = string.Concat(UrlPathSeparator, trimmedPath);
        }

        return trimmedEndpoint + trimmedPath;
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
