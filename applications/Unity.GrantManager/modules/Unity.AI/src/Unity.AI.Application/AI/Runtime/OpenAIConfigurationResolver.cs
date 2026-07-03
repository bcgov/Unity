using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime;

public class OpenAIConfigurationResolver(
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIPrompt, Guid> promptRepository,
    IConfiguration configuration,
    IDataFilter<IMultiTenant> multiTenantDataFilter) : ITransientDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IRepository<AIModel, Guid> _modelRepository = modelRepository;
    private readonly IRepository<AIOperation, Guid> _operationRepository = operationRepository;
    private readonly IRepository<AIPrompt, Guid> _promptRepository = promptRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDataFilter<IMultiTenant> _multiTenantDataFilter = multiTenantDataFilter;

    public string ResolveProviderName() => Required("Azure:Operations:Defaults:Provider");

    public Task<string> ResolveApiKeyAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var providerName = Required("Azure:Operations:Defaults:Provider");
        return Task.FromResult(Required($"Azure:{providerName}:ApiKey"));
    }

    public async Task<OpenAIOperationSettings> ResolveOperationSettingsAsync(
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var operation = await ResolveOperationAsync(operationName, cancellationToken);
        if (operation == null)
        {
            throw new InvalidOperationException($"AI operation '{operationName}' is not configured.");
        }

        var model = await _modelRepository.GetAsync(operation.AIModelId, cancellationToken: cancellationToken);
        if (!model.IsActive)
        {
            throw new InvalidOperationException($"AI model '{model.Name}' is inactive.");
        }

        var modelSettings = ResolveModelSettings(model);
        var providerName = Required("Azure:Operations:Defaults:Provider");
        var endpoint = Required($"Azure:{providerName}:Endpoint");
        var prompt = await LoadPromptAsync(operation.AIPromptId, cancellationToken);
        if (!prompt.IsActive)
        {
            throw new InvalidOperationException($"AI prompt '{prompt.Name}' v{prompt.VersionNumber} is not active.");
        }

        if (operation.CompletionTokens <= 0)
        {
            throw new InvalidOperationException($"AI operation '{operation.Name}' must define a positive CompletionTokens value.");
        }

        var apiKey = Required($"Azure:{providerName}:ApiKey");
        return new OpenAIOperationSettings(
            providerName,
            model.Name,
            apiKey,
            new Uri(endpoint),
            Required($"Azure:{providerName}:Profiles:{model.Name}:DeploymentName"),
            modelSettings.MaxOutputTokenCountSupported,
            modelSettings.Temperature,
            operation.CompletionTokens,
            $"v{prompt.VersionNumber}");
    }

    public async Task<double?> ResolveConfiguredTemperatureAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var modelConfiguration = await ResolveModelConfigurationAsync(modelName, cancellationToken);
        if (modelConfiguration != null)
        {
            return modelConfiguration.Value.Settings.Temperature;
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<bool> ResolveMaxOutputTokenCountSupportedAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var modelConfiguration = await ResolveModelConfigurationAsync(modelName, cancellationToken);
        if (modelConfiguration != null)
        {
            var providerName = Required("Azure:Operations:Defaults:Provider");
            var profileName = modelConfiguration.Value.Model.Name;
            var configuredValue = Optional($"Azure:{providerName}:Profiles:{profileName}:MaxOutputTokenCountSupported");
            if (configuredValue != null)
            {
                if (bool.TryParse(configuredValue, out var parsedValue))
                {
                    return parsedValue;
                }

                throw new InvalidOperationException($"Azure:{providerName}:Profiles:{profileName}:MaxOutputTokenCountSupported is not a valid boolean.");
            }

            return modelConfiguration.Value.Settings.MaxOutputTokenCountSupported;
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<int> ResolveCompletionTokensAsync(string operationName, CancellationToken cancellationToken = default)
    {
        var operation = await ResolveOperationAsync(operationName, cancellationToken);
        if (operation == null)
        {
            throw new InvalidOperationException($"AI operation '{operationName}' is not configured.");
        }

        var model = await _modelRepository.GetAsync(operation.AIModelId, cancellationToken: cancellationToken);
        if (!model.IsActive)
        {
            throw new InvalidOperationException($"AI model '{model.Name}' is inactive.");
        }

        if (operation.CompletionTokens <= 0)
        {
            throw new InvalidOperationException($"AI operation '{operation.Name}' must define a positive CompletionTokens value.");
        }

        return operation.CompletionTokens;
    }

    public Task<string> ResolvePromptVersionAsync(string operationName, CancellationToken cancellationToken = default)
    {
        return ResolvePromptVersionAsyncCore(operationName, cancellationToken);
    }

    public async Task<Uri> ResolveEndpointAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var modelConfiguration = await ResolveModelConfigurationAsync(modelName, cancellationToken);
        if (modelConfiguration != null)
        {
            var providerName = Required("Azure:Operations:Defaults:Provider");
            return new Uri(Required($"Azure:{providerName}:Endpoint"));
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<string> ResolveDeploymentNameAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var modelConfiguration = await ResolveModelConfigurationAsync(modelName, cancellationToken);
        if (modelConfiguration != null)
        {
            var providerName = Required("Azure:Operations:Defaults:Provider");
            return Required($"Azure:{providerName}:Profiles:{modelConfiguration.Value.Model.Name}:DeploymentName");
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<string> ResolveProfileNameAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var modelConfiguration = await ResolveModelConfigurationAsync(modelName, cancellationToken);
        if (modelConfiguration != null)
        {
            return modelConfiguration.Value.Model.Name;
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    private async Task<(AIModel Model, AIModelSettings Settings)?> ResolveModelConfigurationAsync(
        string? modelName,
        CancellationToken cancellationToken)
    {
        var model = await ResolveModelAsync(modelName, cancellationToken);
        if (model == null)
        {
            return null;
        }

        var settings = JsonSerializer.Deserialize<AIModelSettings>(model.SettingsJson, JsonOptions);
        if (settings == null)
        {
            throw new InvalidOperationException($"AI model '{model.Name}' has invalid settings JSON.");
        }

        return (model, settings);
    }

    private async Task<AIOperation?> ResolveOperationAsync(string operationName, CancellationToken cancellationToken)
    {
        var operations = await _operationRepository.GetListAsync(cancellationToken: cancellationToken);
        var configuredOperation = operations.FirstOrDefault(operation =>
            string.Equals(operation.Name, operationName, StringComparison.OrdinalIgnoreCase));
        if (configuredOperation != null)
        {
            if (!configuredOperation.IsActive)
            {
                return null;
            }

            return configuredOperation;
        }

        return null;
    }

    private static AIModelSettings ResolveModelSettings(AIModel model)
    {
        var settings = JsonSerializer.Deserialize<AIModelSettings>(model.SettingsJson, JsonOptions);
        if (settings == null)
        {
            throw new InvalidOperationException($"AI model '{model.Name}' has invalid settings JSON.");
        }

        return settings;
    }

    private async Task<AIModel?> ResolveModelAsync(string? modelName, CancellationToken cancellationToken)
    {
        var activeModels = await _modelRepository.GetListAsync(model => model.IsActive);
        if (activeModels.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            return activeModels.FirstOrDefault(model =>
                string.Equals(model.Name, modelName, StringComparison.OrdinalIgnoreCase));
        }

        var configuredDefaultProfile = Optional("Azure:Operations:Defaults:Profile");
        if (!string.IsNullOrWhiteSpace(configuredDefaultProfile))
        {
            var configuredDefaultModel = activeModels.FirstOrDefault(model =>
                string.Equals(model.Name, configuredDefaultProfile, StringComparison.OrdinalIgnoreCase));
            if (configuredDefaultModel != null)
            {
                return configuredDefaultModel;
            }
        }

        return null;
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

    private async Task<string> ResolvePromptVersionAsyncCore(string operationName, CancellationToken cancellationToken)
    {
        var operation = await ResolveOperationAsync(operationName, cancellationToken);
        if (operation == null)
        {
            throw new InvalidOperationException($"AI operation '{operationName}' is not configured.");
        }

        var prompt = await LoadPromptAsync(operation.AIPromptId, cancellationToken);
        if (!prompt.IsActive)
        {
            throw new InvalidOperationException($"AI prompt '{prompt.Name}' v{prompt.VersionNumber} is not active.");
        }

        return $"v{prompt.VersionNumber}";
    }

    private async Task<AIPrompt> LoadPromptAsync(Guid promptId, CancellationToken cancellationToken)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await _promptRepository.GetAsync(promptId, cancellationToken: cancellationToken);
        }
    }
}
