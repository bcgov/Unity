using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.AI.Domain;
using Unity.AI.Runtime.Prompts;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.DataSeed;

public class AIOperationDataSeeder(
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIModel, Guid> modelRepository,
    ICurrentTenant currentTenant,
    ILogger<AIOperationDataSeeder> logger) : ITransientDependency
{
    private const string DefaultModelName = "gpt-5-mini";

    private static readonly BuiltInOperationDefinition[] BuiltInOperations =
    [
        new(AIPromptTypes.ApplicationAnalysis, 4000),
        new(AIPromptTypes.AttachmentSummary, 2000),
        new(AIPromptTypes.ApplicationScoring, 8000),
        new(AIPromptTypes.FormMapping, 2000),
        new(AIPromptTypes.FormWorksheet, 4000),
        new(AIPromptTypes.FormScoresheet, 4000)
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
        var existing = await operationRepository.FirstOrDefaultAsync(op => op.Name == definition.OperationName);
        if (existing != null)
        {
            existing.AIModelId = model.Id;
            existing.ExecutionMode = definition.ExecutionMode;
            existing.CompletionTokens = definition.CompletionTokens;
            existing.IsActive = true;
            await operationRepository.UpdateAsync(existing, autoSave: true);
            return;
        }

        await operationRepository.InsertAsync(
            new AIOperation(Guid.CreateVersion7(), definition.OperationName, model.Id)
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

    private sealed record BuiltInOperationDefinition(
        string OperationName,
        int CompletionTokens,
        AIExecutionMode ExecutionMode = AIExecutionMode.Sequential);
}
