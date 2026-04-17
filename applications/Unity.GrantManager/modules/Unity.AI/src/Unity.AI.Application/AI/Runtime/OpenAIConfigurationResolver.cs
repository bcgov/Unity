using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIConfigurationResolver(IConfiguration configuration) : ITransientDependency
{
    private const string DefaultMaxTokensParameterName = "max_completion_tokens";
    private const string LegacyMaxTokensParameterName = "max_tokens";
    private const string DefaultProviderName = "OpenAI";
    private const string OpenAiApiKeyEnvironmentVariableName = "AZURE_OPENAI_API_KEY";
    private const string OpenAiEndpointEnvironmentVariableName = "AZURE_OPENAI_ENDPOINT";

    private readonly IConfiguration _configuration = configuration;

    public string ResolveProviderName(string? operationName = null)
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

    public string ResolveApiKey(string? operationName = null)
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

    public string ResolveMaxTokensParameterNameForOperation(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var profileParameterName = ResolveProfileSetting(providerName, profileName, "MaxTokensParameter");
        return ResolveMaxTokensParameterName(profileParameterName);
    }

    public double? ResolveConfiguredTemperature(string? operationName = null)
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

    public int ResolveCompletionTokens(string operationName, int defaultValue)
    {
        var configuredValue = _configuration.GetValue<int?>($"Azure:Operations:{operationName}:MaxCompletionTokens");
        if (configuredValue is > 0)
        {
            return configuredValue.Value;
        }

        var defaultConfiguredValue = _configuration.GetValue<int?>("Azure:Operations:Defaults:MaxCompletionTokens");
        return defaultConfiguredValue is > 0 ? defaultConfiguredValue.Value : defaultValue;
    }

    public string ResolveApiUrl(string? operationName = null)
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

    private static string ResolveMaxTokensParameterName(string? configuredParameterName)
    {
        if (string.Equals(configuredParameterName, LegacyMaxTokensParameterName, StringComparison.Ordinal))
        {
            return LegacyMaxTokensParameterName;
        }

        return DefaultMaxTokensParameterName;
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
}
