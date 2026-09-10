using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public interface IAIGenerationOperationExecutor
{
    string OperationType { get; }

    Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args);
}
