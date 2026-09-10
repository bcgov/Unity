namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public interface IAIGenerationOperationExecutorRegistry
{
    IAIGenerationOperationExecutor Resolve(string operationType);
}
