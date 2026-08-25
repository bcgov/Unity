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
