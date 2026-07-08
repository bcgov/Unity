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
    ICurrentTenant currentTenant,
    ILogger<AIOperationDataSeeder> logger) : ITransientDependency
{
    private const string DefaultModelName = "Gpt5Mini";

    private static readonly BuiltInOperationDefinition[] BuiltInOperations =
    [
        new(AIPromptTypes.ApplicationAnalysis, AIPromptTypes.ApplicationAnalysis, 1, 4000, AIExecutionMode.Single),
        new(AIPromptTypes.AttachmentSummary, AIPromptTypes.AttachmentSummary, 1, 2000),
        new(AIPromptTypes.ApplicationScoring, AIPromptTypes.ApplicationScoring, 1, 8000)
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
        }
    }

    private async Task EnsureOperationAsync(BuiltInOperationDefinition definition, AIModel model)
    {
        var prompt = await ResolvePromptAsync(definition.PromptName, definition.PromptVersionNumber);
        if (prompt == null)
        {
            logger.LogWarning(
                "AI operation seeding skipped: no active prompt found for operation '{OperationName}' and prompt '{PromptName}' version '{PromptVersionNumber}'.",
                definition.OperationName,
                definition.PromptName,
                definition.PromptVersionNumber);
            return;
        }

        var existing = await operationRepository.FirstOrDefaultAsync(op => op.Name == definition.OperationName);
        if (existing != null)
        {
            existing.AIModelId = model.Id;
            existing.AIPromptId = prompt.Id;
            existing.ExecutionMode = definition.ExecutionMode;
            existing.CompletionTokens = definition.CompletionTokens;
            existing.IsActive = true;
            await operationRepository.UpdateAsync(existing, autoSave: true);
            return;
        }

        await operationRepository.InsertAsync(
            new AIOperation(Guid.CreateVersion7(), definition.OperationName, model.Id, prompt.Id)
            {
                ExecutionMode = definition.ExecutionMode,
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

    private async Task<AIPrompt?> ResolvePromptAsync(string promptName, int promptVersionNumber)
    {
        return await promptRepository.FirstOrDefaultAsync(item =>
            item.Name == promptName &&
            item.VersionNumber == promptVersionNumber &&
            item.IsActive);
    }

    private sealed record BuiltInOperationDefinition(
        string OperationName,
        string PromptName,
        int PromptVersionNumber,
        int CompletionTokens,
        AIExecutionMode ExecutionMode = AIExecutionMode.Sequential);
}
