using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.AI.Runtime.Execution;
using Unity.AI.Runtime.Prompts;
using Unity.AI.Setup;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Unity.AI.Domain;

/// <summary>
/// Rules for the host-wide AI models and operations managed in Unity AI Setup.
/// </summary>
public class AISetupManager(
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIOperation, Guid> operationRepository,
    AIConfigurationCacheSignal cacheSignal) : DomainService
{
    /// <summary>The case-insensitive unique index on AIModels.Name (PostgreSQL).</summary>
    public const string NameLowerIndexName = "IX_AIModels_Name_Lower";

    public virtual async Task<AIModel> CreateModelAsync(
        string name,
        string provider,
        string type,
        string apiVersion,
        bool isActive)
    {
        var normalizedName = NormalizeName(name);
        EnsureProviderSupported(provider);
        var modelType = ParseModelType(type);
        var normalizedApiVersion = EnsureApiVersionSupported(apiVersion);
        await EnsureModelNameUniqueAsync(normalizedName, null);

        var model = new AIModel(GuidGenerator.Create(), normalizedName, provider)
        {
            Type = modelType,
            ApiVersion = normalizedApiVersion,
            IsActive = isActive
        };

        model = await SaveModelAsync(normalizedName, () => modelRepository.InsertAsync(model, autoSave: true));
        cacheSignal.Reset();
        return model;
    }

    public virtual async Task<AIModel> UpdateModelAsync(
        AIModel model,
        string name,
        string provider,
        string type,
        string apiVersion,
        bool isActive)
    {
        var normalizedName = NormalizeName(name);
        EnsureProviderSupported(provider);
        var modelType = ParseModelType(type);
        var normalizedApiVersion = EnsureApiVersionSupported(apiVersion);
        await EnsureModelNameUniqueAsync(normalizedName, model.Id);

        if ((model.IsActive && !isActive) || model.Type != modelType)
        {
            // Deactivating, or changing what kind of deployment it is, would break whatever uses it.
            await EnsureModelNotInUseAsync(model);
        }

        model.Name = normalizedName;
        model.Provider = provider;
        model.Type = modelType;
        model.ApiVersion = normalizedApiVersion;
        model.IsActive = isActive;

        model = await SaveModelAsync(normalizedName, () => modelRepository.UpdateAsync(model, autoSave: true));
        cacheSignal.Reset();
        return model;
    }

    public virtual async Task DeleteModelAsync(AIModel model)
    {
        await EnsureModelNotInUseAsync(model);
        await modelRepository.DeleteAsync(model, autoSave: true);
        cacheSignal.Reset();
    }

    /// <summary>
    /// Updates an operation, creating its row when the built-in operation is not configured yet.
    /// </summary>
    public virtual async Task<AIOperation> SetOperationAsync(
        string operationName,
        Guid modelId,
        string executionMode,
        bool isActive)
    {
        var existing = await FindOperationAsync(operationName);
        if (existing == null && FindBuiltInName(operationName) == null)
        {
            throw new BusinessException(AIErrorCodes.SetupOperationUnknown)
                .WithData("name", operationName);
        }

        var model = await modelRepository.FindAsync(modelId);
        if (model == null || !model.IsActive)
        {
            throw new BusinessException(AIErrorCodes.SetupModelInactive);
        }

        var requiredType = GetRequiredModelType(operationName);
        if (model.Type != requiredType)
        {
            throw new BusinessException(requiredType == AIModelType.Embedding
                    ? AIErrorCodes.SetupModelNotEmbedding
                    : AIErrorCodes.SetupModelNotChat)
                .WithData("name", model.Name);
        }

        if (!Enum.TryParse<AIExecutionMode>(executionMode, ignoreCase: true, out var mode)
            || !Enum.IsDefined(mode))
        {
            throw new BusinessException(AIErrorCodes.SetupExecutionModeInvalid)
                .WithData("mode", executionMode);
        }

        if (mode != AIExecutionMode.Sequential
            && !AIExecutionModeResolver.OperationsUsingExecutionMode.Contains(operationName, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException(AIErrorCodes.SetupExecutionModeNotUsed)
                .WithData("name", operationName);
        }

        AIOperation operation;
        if (existing == null)
        {
            var builtInName = FindBuiltInName(operationName)!;
            operation = new AIOperation(GuidGenerator.Create(), builtInName, model.Id)
            {
                ExecutionMode = mode,
                IsActive = isActive
            };
            operation = await operationRepository.InsertAsync(operation, autoSave: true);
        }
        else
        {
            existing.AIModelId = model.Id;
            existing.ExecutionMode = mode;
            existing.IsActive = isActive;
            operation = await operationRepository.UpdateAsync(existing, autoSave: true);
        }

        cacheSignal.Reset();
        return operation;
    }

    public virtual async Task<AIOperation?> FindOperationAsync(string operationName)
    {
        var operations = await operationRepository.GetListAsync();
        return operations.FirstOrDefault(operation =>
            string.Equals(operation.Name, operationName, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessException(AIErrorCodes.SetupModelNameRequired);
        }

        return name.Trim();
    }

    /// <summary>
    /// The built-in operation names: the six prompt operations and AI Reporting's two rows.
    /// </summary>
    public static string? FindBuiltInName(string operationName)
    {
        return AIPromptTypes.All
            .Concat(AISetupConsts.AIReportingOperations)
            .FirstOrDefault(name => string.Equals(name, operationName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>AI Reporting's embedding row needs an Embedding model; every other operation a Chat model.</summary>
    public static AIModelType GetRequiredModelType(string operationName)
    {
        return string.Equals(operationName, AISetupConsts.AIReportingEmbeddingOperation, StringComparison.OrdinalIgnoreCase)
            ? AIModelType.Embedding
            : AIModelType.Chat;
    }

    /// <summary>
    /// A well-formed Azure OpenAI API version (2024-10-21, 2025-04-01-preview). Whether Azure
    /// accepts it for the deployment shows on Test.
    /// </summary>
    private static string EnsureApiVersionSupported(string apiVersion)
    {
        var trimmed = apiVersion?.Trim() ?? string.Empty;
        if (!AzureOpenAIApiVersions.IsValid(trimmed))
        {
            throw new BusinessException(AIErrorCodes.SetupApiVersionInvalid)
                .WithData("version", trimmed);
        }

        return trimmed;
    }

    private static AIModelType ParseModelType(string type)
    {
        if (!Enum.TryParse<AIModelType>(type, ignoreCase: true, out var modelType) || !Enum.IsDefined(modelType))
        {
            throw new BusinessException(AIErrorCodes.SetupModelTypeInvalid)
                .WithData("type", type ?? string.Empty);
        }

        return modelType;
    }

    /// <summary>
    /// Saves, turning a hit on the case-insensitive name index (two saves at the same moment)
    /// into the usual duplicate-name error.
    /// </summary>
    private static async Task<AIModel> SaveModelAsync(string name, Func<Task<AIModel>> save)
    {
        try
        {
            return await save();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains(NameLowerIndexName, StringComparison.Ordinal) == true)
        {
            throw new BusinessException(AIErrorCodes.SetupModelNameDuplicate)
                .WithData("name", name);
        }
    }

    private static void EnsureProviderSupported(string provider)
    {
        if (!AISetupConsts.SupportedProviders.Contains(provider, StringComparer.Ordinal))
        {
            throw new BusinessException(AIErrorCodes.SetupProviderNotSupported)
                .WithData("provider", provider ?? string.Empty);
        }
    }

    private async Task EnsureModelNameUniqueAsync(string name, Guid? currentModelId)
    {
        var models = await modelRepository.GetListAsync();
        if (models.Any(model =>
                model.Id != currentModelId
                && string.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException(AIErrorCodes.SetupModelNameDuplicate)
                .WithData("name", name);
        }
    }

    private async Task EnsureModelNotInUseAsync(AIModel model)
    {
        var operations = await operationRepository.GetListAsync(operation => operation.AIModelId == model.Id);
        if (operations.Count > 0)
        {
            throw new BusinessException(AIErrorCodes.SetupModelInUse)
                .WithData("name", model.Name)
                .WithData("operations", string.Join(", ", operations.Select(operation => operation.Name).OrderBy(n => n)));
        }
    }
}
