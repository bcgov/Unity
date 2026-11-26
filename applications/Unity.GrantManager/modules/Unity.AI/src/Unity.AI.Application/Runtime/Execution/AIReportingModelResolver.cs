using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Setup;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// The Azure deployments AI Reporting should use, each with its own API version; null where AI
/// Setup has none usable.
/// </summary>
public sealed record AIReportingModels(
    string? ChatDeployment,
    string? ChatApiVersion,
    string? EmbeddingDeployment,
    string? EmbeddingApiVersion)
{
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ChatDeployment) && !string.IsNullOrWhiteSpace(ChatApiVersion)
        && !string.IsNullOrWhiteSpace(EmbeddingDeployment) && !string.IsNullOrWhiteSpace(EmbeddingApiVersion);
}

/// <summary>
/// Reads AI Reporting's model choices (the AIReporting and AIReportingEmbedding operation rows)
/// from AI Setup, with each model's API version. UGM sends them to UAI in the AI Reporting
/// token; UAI has no defaults of its own.
/// </summary>
public class AIReportingModelResolver(
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIModel, Guid> modelRepository) : ITransientDependency
{
    public virtual async Task<AIReportingModels> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var operations = await operationRepository.GetListAsync(
            operation => operation.IsActive,
            cancellationToken: cancellationToken);
        var models = (await modelRepository.GetListAsync(
            model => model.IsActive,
            cancellationToken: cancellationToken)).ToDictionary(model => model.Id);

        var chat = FindModel(operations, models, AISetupConsts.AIReportingChatOperation, AIModelType.Chat);
        var embedding = FindModel(operations, models, AISetupConsts.AIReportingEmbeddingOperation, AIModelType.Embedding);

        return new AIReportingModels(
            chat?.Name,
            string.IsNullOrWhiteSpace(chat?.ApiVersion) ? null : chat.ApiVersion,
            embedding?.Name,
            string.IsNullOrWhiteSpace(embedding?.ApiVersion) ? null : embedding.ApiVersion);
    }

    private static AIModel? FindModel(
        IEnumerable<AIOperation> operations,
        IReadOnlyDictionary<Guid, AIModel> activeModels,
        string operationName,
        AIModelType requiredType)
    {
        var operation = operations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, operationName, StringComparison.OrdinalIgnoreCase));
        if (operation == null
            || !activeModels.TryGetValue(operation.AIModelId, out var model)
            || model.Type != requiredType)
        {
            return null;
        }

        return model;
    }
}
