using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Unity.AI.Generation;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class AIGenerationOperationExecutorRegistry(IServiceProvider serviceProvider)
    : IAIGenerationOperationExecutorRegistry, ITransientDependency
{
    public IAIGenerationOperationExecutor Resolve(string operationType)
    {
        var operation = AIGenerationOperations.Get(operationType);
        var executors = serviceProvider.GetServices<IAIGenerationOperationExecutor>();
        var executor = executors.SingleOrDefault(x =>
            string.Equals(x.OperationType, operation.OperationType, StringComparison.Ordinal));

        return executor
            ?? throw new UserFriendlyException($"No AI generation executor is registered for operation '{operationType}'.");
    }
}
