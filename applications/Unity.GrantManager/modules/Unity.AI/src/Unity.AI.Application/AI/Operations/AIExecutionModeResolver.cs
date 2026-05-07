using Microsoft.Extensions.Configuration;
using Unity.AI.Prompts;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

/// <summary>
/// Resolves the configured <see cref="AIExecutionMode"/> for an AI operation.
/// Configuration keys (all optional, default = Sequential):
///   Azure:Operations:{operationName}:ExecutionMode - "Sequential" | "Parallel" | "Batch" (case-insensitive)
///   Azure:Operations:Defaults:ExecutionMode        - default when operation override is absent
/// </summary>
public class AIExecutionModeResolver(IConfiguration configuration) : ITransientDependency
{
    public const string AttachmentSummaryOperation = AIPromptTypes.AttachmentSummary;
    public const string ApplicationScoringOperation = AIPromptTypes.ApplicationScoring;

    public AIExecutionMode ResolveMode(string operationName)
    {
        var configured = configuration[$"Azure:Operations:{operationName}:ExecutionMode"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = configuration["Azure:Operations:Defaults:ExecutionMode"];
        }

        return configured?.Trim().ToLowerInvariant() switch
        {
            "parallel" => AIExecutionMode.Parallel,
            "batch" => AIExecutionMode.Batch,
            _ => AIExecutionMode.Sequential
        };
    }
}
