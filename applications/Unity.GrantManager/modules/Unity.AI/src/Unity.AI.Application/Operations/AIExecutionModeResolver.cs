using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Runtime.Prompts;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.Operations;

/// <summary>
/// Resolves the persisted <see cref="AIExecutionMode"/> for an active AI operation.
/// </summary>
public class AIExecutionModeResolver(
    IRepository<AIOperation, Guid> operationRepository) : ITransientDependency
{
    public const string AttachmentSummaryOperation = AIPromptTypes.AttachmentSummary;
    public const string ApplicationScoringOperation = AIPromptTypes.ApplicationScoring;
    public const string FormMappingOperation = AIPromptTypes.FormMapping;
    public const string FormWorksheetOperation = AIPromptTypes.FormWorksheet;
    public const string FormScoresheetOperation = AIPromptTypes.FormScoresheet;

    /// <summary>
    /// Operations that read their execution mode. The others make a single call, or
    /// (AttachmentSummary) always run one item at a time. Keep in step with the
    /// callers of <see cref="ResolveModeAsync"/>; Unity AI Setup shows the mode only for these.
    /// </summary>
    public static readonly string[] OperationsUsingExecutionMode = [ApplicationScoringOperation];

    public async Task<AIExecutionMode> ResolveModeAsync(
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var operations = await operationRepository.GetListAsync(
            candidate => candidate.IsActive,
            cancellationToken: cancellationToken);
        var operation = operations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, operationName, StringComparison.OrdinalIgnoreCase));
        if (operation == null)
        {
            throw new InvalidOperationException($"AI operation '{operationName}' is not configured.");
        }

        return operation.ExecutionMode;
    }
}
