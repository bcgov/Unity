using Microsoft.Extensions.Configuration;
using System;
using Unity.AI.Prompts;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

/// <summary>
/// Resolves the configured <see cref="AIExecutionMode"/> for an AI operation.
/// Configuration keys:
///   Azure:Operations:{operationName}:ExecutionMode - "Sequential" | "Parallel" | "Batch" (case-insensitive)
///   Azure:Operations:Defaults:ExecutionMode        - required default when operation override is absent
/// </summary>
public class AIExecutionModeResolver(IConfiguration configuration) : ITransientDependency
{
    public const string AttachmentSummaryOperation = AIPromptTypes.AttachmentSummary;
    public const string ApplicationScoringOperation = AIPromptTypes.ApplicationScoring;
    public const string FormMappingOperation = AIPromptTypes.FormMapping;
    public const string FormWorksheetOperation = AIPromptTypes.FormWorksheet;

    public AIExecutionMode ResolveMode(string operationName)
    {
        var configured = configuration[$"Azure:Operations:{operationName}:ExecutionMode"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = configuration["Azure:Operations:Defaults:ExecutionMode"];
        }

        return configured?.Trim().ToLowerInvariant() switch
        {
            "sequential" => AIExecutionMode.Sequential,
            "parallel" => AIExecutionMode.Parallel,
            "batch" => AIExecutionMode.Batch,
            _ => throw new InvalidOperationException($"AI execution mode is not configured or is invalid for operation '{operationName}'.")
        };
    }
}
