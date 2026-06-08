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

    public OpenAIOperationSettings ResolveOperationSettings(string operationName)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var apiKey = Required($"Azure:{providerName}:ApiKey");
        var endpoint = new Uri(Required($"Azure:{providerName}:Endpoint"));
        var deploymentName = RequiredProfile(providerName, profileName, "DeploymentName");
        var promptVersion = Optional($"Azure:Operations:{operationName}:PromptVersion")
            ?? Required("Azure:Operations:Defaults:PromptVersion");

        return new OpenAIOperationSettings(
            providerName,
            profileName,
            apiKey,
            endpoint,
            deploymentName,
            ResolveMaxOutputTokenCountSupported(operationName),
            ResolveConfiguredTemperature(operationName),
            ResolveCompletionTokens(operationName),
            promptVersion);
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

    public bool ResolveMaxOutputTokenCountSupported(string? operationName = null)
    {
        var providerName = ResolveProviderName(operationName);
        var profileName = ResolveProfileName(operationName);
        var key = ProfileKey(providerName, profileName, "MaxOutputTokenCountSupported");
        var configuredValue = Optional(key);
        if (configuredValue == null)
        {
            return true;
        }

        if (bool.TryParse(configuredValue, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"{key} must be 'true' or 'false'.");
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
        return RequiredProfile(providerName, profileName, "DeploymentName");
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
