using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Prompts;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.DataSeed;

public class AIOperationDataSeeder(
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIPrompt, Guid> promptRepository,
    IRepository<AIGenerationRequest, Guid> requestRepository,
    ICurrentTenant currentTenant,
    ILogger<AIOperationDataSeeder> logger) : ITransientDependency
{
    private const string DefaultModelName = "Gpt5Mini";

    private static readonly BuiltInOperationDefinition[] BuiltInOperations =
    [
        new("Default", AIPromptTypes.ApplicationAnalysis, 4000, true),
        new(AIPromptTypes.ApplicationAnalysis, AIPromptTypes.ApplicationAnalysis, 4000),
        new(AIPromptTypes.AttachmentSummary, AIPromptTypes.AttachmentSummary, 2000),
        new(AIPromptTypes.ApplicationScoring, AIPromptTypes.ApplicationScoring, 8000)
    ];

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null)
        {
            return;
        }

        using (currentTenant.Change(null))
        {
            var model = await EnsureModelAsync(DefaultModelName);
            if (model == null)
            {
                logger.LogWarning("AI operation seeding skipped: model '{ModelName}' is missing.", DefaultModelName);
                return;
            }

            foreach (var definition in BuiltInOperations)
            {
                await EnsureOperationAsync(definition, model);
            }

            await BackfillRequestOperationIdsAsync();
        }
    }

    private async Task EnsureOperationAsync(BuiltInOperationDefinition definition, AIModel model)
    {
        var prompt = await ResolveLatestActivePromptAsync(definition.PromptName);
        if (prompt == null)
        {
            if (definition.IsDefaultOperation)
            {
                logger.LogWarning("AI default operation seeding skipped: no active prompt found for '{PromptName}'.", definition.PromptName);
            }
            else
            {
                logger.LogWarning("AI operation seeding skipped: no active prompt found for operation '{OperationName}' and prompt '{PromptName}'.", definition.OperationName, definition.PromptName);
            }
            return;
        }

        var existing = await operationRepository.FirstOrDefaultAsync(op => op.Name == definition.OperationName);
        if (existing != null)
        {
            existing.AIModelId = model.Id;
            existing.AIPromptId = prompt.Id;
            existing.ExecutionMode = AIExecutionMode.Sequential;
            existing.CompletionTokens = definition.CompletionTokens;
            existing.IsActive = true;
            await operationRepository.UpdateAsync(existing);
            return;
        }

        await operationRepository.InsertAsync(
            new AIOperation(Guid.CreateVersion7(), definition.OperationName, model.Id, prompt.Id)
            {
                ExecutionMode = AIExecutionMode.Sequential,
                CompletionTokens = definition.CompletionTokens,
                IsActive = true
            },
            autoSave: true);
    }

    private async Task<AIModel?> EnsureModelAsync(string modelName)
    {
        var models = await modelRepository.GetListAsync(model => model.Name == modelName && model.IsActive);
        return models.FirstOrDefault();
    }

    private async Task<AIPrompt?> ResolveLatestActivePromptAsync(string promptName)
    {
        var prompts = await promptRepository.GetListAsync(item => item.Name == promptName && item.IsActive);
        return prompts
            .OrderByDescending(prompt => prompt.VersionNumber)
            .FirstOrDefault();
    }

    private async Task BackfillRequestOperationIdsAsync()
    {
        var requests = await requestRepository.GetListAsync(request => request.OperationId == null);
        if (requests.Count == 0)
        {
            return;
        }

        var operations = await operationRepository.GetListAsync();
        var operationsByName = operations.ToDictionary(operation => operation.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            var operationType = ResolveOperationTypeFromRequestKey(request.RequestKey);
            if (operationType == null)
            {
                continue;
            }

            var operationName = AIGenerationRequestKeyHelper.ResolveOperationName(operationType);
            if (operationName == null || !operationsByName.TryGetValue(operationName, out var operation))
            {
                continue;
            }

            request.OperationId = operation.Id;
            await requestRepository.UpdateAsync(request, autoSave: true);
        }
    }

    private static string? ResolveOperationTypeFromRequestKey(string requestKey)
    {
        var parts = requestKey.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 3 ? parts[2] : null;
    }

    private sealed record BuiltInOperationDefinition(
        string OperationName,
        string PromptName,
        int CompletionTokens,
        bool IsDefaultOperation = false);
}
