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

namespace Unity.AI.Domain;

/// <summary>
/// Rules for the host-wide AI models and operations managed in Unity AI Setup.
/// </summary>
public class AISetupManager(
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIOperation, Guid> operationRepository,
    AIConfigurationCacheSignal cacheSignal) : DomainService
{
    public virtual async Task<AIModel> CreateModelAsync(
        string name,
        string provider,
        bool isActive)
    {
        var normalizedName = NormalizeName(name);
        EnsureProviderSupported(provider);
        await EnsureModelNameUniqueAsync(normalizedName, null);

        var model = new AIModel(GuidGenerator.Create(), normalizedName, provider)
        {
            IsActive = isActive
        };

        model = await modelRepository.InsertAsync(model, autoSave: true);
        cacheSignal.Reset();
        return model;
    }

    public virtual async Task<AIModel> UpdateModelAsync(
        AIModel model,
        string name,
        string provider,
        bool isActive)
    {
        var normalizedName = NormalizeName(name);
        EnsureProviderSupported(provider);
        await EnsureModelNameUniqueAsync(normalizedName, model.Id);

        if (model.IsActive && !isActive)
        {
            await EnsureModelNotInUseAsync(model);
        }

        model.Name = normalizedName;
        model.Provider = provider;
        model.IsActive = isActive;

        model = await modelRepository.UpdateAsync(model, autoSave: true);
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
        if (existing == null && !AIPromptTypes.All.Contains(operationName, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException(AIErrorCodes.SetupOperationUnknown)
                .WithData("name", operationName);
        }

        var model = await modelRepository.FindAsync(modelId);
        if (model == null || !model.IsActive)
        {
            throw new BusinessException(AIErrorCodes.SetupModelInactive);
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
            var builtInName = AIPromptTypes.All.First(name =>
                string.Equals(name, operationName, StringComparison.OrdinalIgnoreCase));
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
