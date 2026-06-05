using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIConfigurationResolver(IConfiguration configuration) : ITransientDependency
{
    private readonly IConfiguration _configuration = configuration;

    public string ResolveProviderName(string? operationName = null)
    {
        if (!string.IsNullOrWhiteSpace(operationName))
        {
            var operationProvider = Optional($"Azure:Operations:{operationName}:Provider");
            if (operationProvider != null)
            {
                return operationProvider;
            }
        }

        return Required("Azure:Operations:Defaults:Provider");
    }

    public string ResolveApiKey(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        return Required($"Azure:{providerName}:ApiKey");
    }

    public double? ResolveConfiguredTemperature(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var profileTemperature = OptionalProfile(providerName, profileName, "Temperature");
        if (profileTemperature != null
            && double.TryParse(profileTemperature, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTemperature))
        {
            return parsedTemperature;
        }

        return null;
    }

    public int ResolveCompletionTokens(string operationName)
    {
        var configuredValue = OptionalPositiveInt($"Azure:Operations:{operationName}:MaxCompletionTokens");
        if (configuredValue is > 0)
        {
            return configuredValue.Value;
        }

        var defaultConfiguredValue = OptionalPositiveInt("Azure:Operations:Defaults:MaxCompletionTokens");
        if (defaultConfiguredValue is > 0)
        {
            return defaultConfiguredValue.Value;
        }

        throw new InvalidOperationException($"AI max completion tokens are not configured for operation '{operationName}'.");
    }

    public string ResolvePromptVersion(string operationName)
    {
        return Optional($"Azure:Operations:{operationName}:PromptVersion")
            ?? Required("Azure:Operations:Defaults:PromptVersion");
    }

    public Uri ResolveEndpoint(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var endpoint = Required($"Azure:{providerName}:Endpoint");
        return new Uri(endpoint);
    }

    public string ResolveDeploymentName(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var profileApiUrl = RequiredProfile(providerName, profileName, "ApiUrl");
        // Extract deployment name from URL like "/openai/deployments/gpt-5-mini/chat/completions?api-version=..."
        var parts = profileApiUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var deploymentIndex = Array.IndexOf(parts, "deployments");
        if (deploymentIndex >= 0 && deploymentIndex + 1 < parts.Length)
        {
            var raw = parts[deploymentIndex + 1];
            // Strip query string if present (e.g., "gpt-5-mini?api-version=...")
            var deployment = raw.Contains('?') ? raw[..raw.IndexOf('?')] : raw;
            return deployment;
        }
        throw new InvalidOperationException($"Could not extract deployment name from API URL: {profileApiUrl}");
    }

    public string? ResolveApiVersion(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var profileApiUrl = RequiredProfile(providerName, profileName, "ApiUrl");
        // Extract api-version from query string like "?api-version=2024-10-01-preview"
        var queryString = profileApiUrl.Contains('?') ? profileApiUrl.Substring(profileApiUrl.IndexOf('?') + 1) : null;
        if (string.IsNullOrEmpty(queryString))
        {
            return null;
        }

        var parameters = queryString.Split('&');
        foreach (var param in parameters)
        {
            var keyValue = param.Split('=');
            if (keyValue.Length == 2 && keyValue[0] == "api-version")
            {
                return Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return null;
    }

    private string ResolveProfileName(string? operationName)
    {
        if (!string.IsNullOrWhiteSpace(operationName))
        {
            var operationProfile = Optional($"Azure:Operations:{operationName}:Profile");
            if (operationProfile != null)
            {
                return operationProfile;
            }
        }

        return Required("Azure:Operations:Defaults:Profile");
    }

    private string RequiredProfile(string providerName, string profileName, string settingName)
    {
        var key = ProfileKey(providerName, profileName, settingName);
        return Required(key);
    }

    private string? OptionalProfile(string providerName, string profileName, string settingName)
    {
        return Optional(ProfileKey(providerName, profileName, settingName));
    }

    private string Required(string key)
    {
        return Optional(key) ?? throw new InvalidOperationException($"{key} is not configured.");
    }

    private string? Optional(string key)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private int? OptionalPositiveInt(string key)
    {
        var value = _configuration.GetValue<int?>(key);
        return value is > 0 ? value : null;
    }

    private static string ProfileKey(string providerName, string profileName, string settingName)
    {
        return $"Azure:{providerName}:Profiles:{profileName}:{settingName}";
    }
}
