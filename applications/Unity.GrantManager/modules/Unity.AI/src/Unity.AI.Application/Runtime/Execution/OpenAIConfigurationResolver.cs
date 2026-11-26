using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime.Execution;

public class OpenAIConfigurationResolver(
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIPrompt, Guid> promptRepository,
    IMemoryCache memoryCache,
    IConfiguration configuration,
    IDataFilter<IMultiTenant> multiTenantDataFilter,
    ICurrentTenant currentTenant,
    AIConfigurationCacheSignal cacheSignal) : ITransientDependency
{
    private const string DefaultProviderName = "OpenAI";
    private static readonly TimeSpan SettingsCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IRepository<AIModel, Guid> _modelRepository = modelRepository;
    private readonly IRepository<AIOperation, Guid> _operationRepository = operationRepository;
    private readonly IRepository<AIPrompt, Guid> _promptRepository = promptRepository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDataFilter<IMultiTenant> _multiTenantDataFilter = multiTenantDataFilter;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly AIConfigurationCacheSignal _cacheSignal = cacheSignal;

    public string ResolveProviderName() => DefaultProviderName;

    public Task<string> ResolveApiKeyAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Required($"Azure:{DefaultProviderName}:ApiKey"));
    }

    public async Task<OpenAIOperationSettings> ResolveOperationSettingsAsync(
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildOperationSettingsCacheKey(operationName);
        if (_memoryCache.TryGetValue(cacheKey, out OpenAIOperationSettings? cachedSettings) && cachedSettings is not null)
        {
            return cachedSettings;
        }

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

        if (model.Type != AIModelType.Chat)
        {
            throw new InvalidOperationException($"AI model '{model.Name}' is not a chat model.");
        }

        var providerName = model.Provider;
        if (!string.Equals(providerName, DefaultProviderName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"AI provider '{providerName}' is not supported.");
        }

        var endpoint = Required($"Azure:{providerName}:Endpoint");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Azure:{providerName}:Endpoint must be a valid absolute URI.");
        }
        var prompt = await ResolvePromptAsync(operation.Name, cancellationToken);
        if (!prompt.IsActive)
        {
            throw new InvalidOperationException($"AI prompt '{prompt.Name}' v{prompt.VersionNumber} is not active.");
        }

        var apiKey = Required($"Azure:{providerName}:ApiKey");
        var apiVersion = model.ApiVersion;
        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException($"AI model '{model.Name}' has no API version set in AI Setup.");
        }

        var settings = new OpenAIOperationSettings(
            providerName,
            model.Name,
            apiKey,
            new Uri(endpoint),
            model.Name,
            apiVersion.Trim(),
            $"v{prompt.VersionNumber}");

        _memoryCache.Set(cacheKey, settings, CreateCacheEntryOptions());
        return settings;
    }

    public Task<string> ResolvePromptVersionAsync(string operationName, CancellationToken cancellationToken = default)
    {
        return ResolvePromptVersionAsyncCore(operationName, cancellationToken);
    }

    public async Task<Uri> ResolveEndpointAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var model = await ResolveModelAsync(modelName, cancellationToken);
        if (model != null)
        {
            return new Uri(Required($"Azure:{model.Provider}:Endpoint"));
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<string> ResolveDeploymentNameAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var model = await ResolveModelAsync(modelName, cancellationToken);
        if (model != null)
        {
            return model.Name;
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    public async Task<string> ResolveProfileNameAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        var model = await ResolveModelAsync(modelName, cancellationToken);
        if (model != null)
        {
            return model.Name;
        }

        throw new InvalidOperationException("AI model is not configured.");
    }

    private async Task<AIModel?> ResolveModelAsync(string? modelName, CancellationToken cancellationToken)
    {
        var activeModels = await _modelRepository.GetListAsync(model => model.IsActive, cancellationToken: cancellationToken);
        if (activeModels.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            return activeModels.FirstOrDefault(model =>
                string.Equals(model.Name, modelName, StringComparison.OrdinalIgnoreCase));
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

        var prompt = await ResolvePromptAsync(operation.Name, cancellationToken);
        if (!prompt.IsActive)
        {
            throw new InvalidOperationException($"AI prompt '{prompt.Name}' v{prompt.VersionNumber} is not active.");
        }

        return $"v{prompt.VersionNumber}";
    }

    private string BuildOperationSettingsCacheKey(string operationName)
    {
        return $"ai:operation-settings:{_currentTenant.Id?.ToString() ?? "host"}:{operationName.Trim().ToLowerInvariant()}";
    }

    private async Task<AIPrompt> LoadPromptAsync(Guid promptId, CancellationToken cancellationToken)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await _promptRepository.GetAsync(promptId, cancellationToken: cancellationToken);
        }
    }

    private async Task<ResolvedOperationSnapshot?> ResolveOperationAsync(string operationName, CancellationToken cancellationToken)
    {
        var cacheKey = BuildOperationSnapshotCacheKey(operationName);
        if (_memoryCache.TryGetValue(cacheKey, out ResolvedOperationSnapshot? cachedOperation) && cachedOperation is not null)
        {
            return cachedOperation;
        }

        var operations = await _operationRepository.GetListAsync(
            operation => operation.IsActive,
            cancellationToken: cancellationToken);

        var operation = operations.FirstOrDefault(operation =>
            string.Equals(operation.Name, operationName, StringComparison.OrdinalIgnoreCase));

        if (operation == null)
        {
            return null;
        }

        var snapshot = new ResolvedOperationSnapshot(
            operation.Id,
            operation.Name,
            operation.AIModelId,
            operation.IsActive);

        _memoryCache.Set(cacheKey, snapshot, CreateCacheEntryOptions());
        return snapshot;
    }

    private MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(SettingsCacheDuration)
            .AddExpirationToken(_cacheSignal.GetChangeToken());
    }

    private string BuildOperationSnapshotCacheKey(string operationName)
    {
        return $"ai:operation-snapshot:{_currentTenant.Id?.ToString() ?? "host"}:{operationName.Trim().ToLowerInvariant()}";
    }

    private async Task<AIPrompt> ResolvePromptAsync(string promptFamily, CancellationToken cancellationToken)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompts = await _promptRepository.GetListAsync(
                prompt => prompt.Name == promptFamily && prompt.IsActive,
                cancellationToken: cancellationToken);
            prompts ??= [];

            var selected = _currentTenant.Id is Guid tenantId
                ? prompts.Where(prompt => prompt.TenantId == tenantId)
                    .OrderByDescending(prompt => prompt.VersionNumber)
                    .FirstOrDefault()
                : null;

            selected ??= prompts.Where(prompt => prompt.TenantId == null)
                .OrderByDescending(prompt => prompt.VersionNumber)
                .FirstOrDefault();

            return selected
                ?? throw new InvalidOperationException(
                    $"AI prompt family '{promptFamily}' has no active prompt for the current tenant.");
        }
    }

    private sealed record ResolvedOperationSnapshot(
        Guid Id,
        string Name,
        Guid AIModelId,
        bool IsActive);
}
