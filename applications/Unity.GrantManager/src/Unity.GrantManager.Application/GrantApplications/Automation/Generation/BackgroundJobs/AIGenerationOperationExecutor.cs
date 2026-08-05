using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public abstract class AIGenerationOperationExecutor : IAIGenerationOperationExecutor
{
    public abstract string OperationType { get; }

    Task<bool> IAIGenerationOperationExecutor.ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        return ExecuteAsync(args);
    }

    protected abstract Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args);
}
