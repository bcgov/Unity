using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

/// <summary>
/// Resolves the configured <see cref="AIExecutionMode"/> and batch size for a given flow.
/// Configuration keys (all optional, default = Sequential / batch size 5):
///   Azure:AIExecutionModes:{flowKey}  - "Sequential" | "Parallel" | "Batch" (case-insensitive)
///   Azure:AIExecutionModes:BatchSize  - positive int, used only by Batch mode
/// </summary>
public class AIExecutionModeResolver(IConfiguration configuration) : ITransientDependency
{
    public const string AttachmentSummariesFlow = "AttachmentSummaries";
    public const string ScoresheetFlow = "Scoresheet";

    private const string ModeKeyPrefix = "Azure:AIExecutionModes:";
    private const string BatchSizeKey = "Azure:AIExecutionModes:BatchSize";
    private const int DefaultBatchSize = 5;

    public AIExecutionMode ResolveMode(string flowKey)
    {
        var configured = configuration[ModeKeyPrefix + flowKey];
        return configured?.Trim().ToLowerInvariant() switch
        {
            "parallel" => AIExecutionMode.Parallel,
            "batch" => AIExecutionMode.Batch,
            _ => AIExecutionMode.Sequential
        };
    }

    public int ResolveBatchSize()
    {
        return int.TryParse(configuration[BatchSizeKey], out var size) && size > 0
            ? size
            : DefaultBatchSize;
    }
}
