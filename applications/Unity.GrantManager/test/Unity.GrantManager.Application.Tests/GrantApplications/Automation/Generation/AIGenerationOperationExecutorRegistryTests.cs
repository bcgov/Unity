using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Unity.AI.Generation;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp;
using Xunit;

namespace Unity.GrantManager.AI;

public class AIGenerationOperationExecutorRegistryTests
{
    [Fact]
    public void Resolve_Should_Return_Executor_For_Operation_Type()
    {
        var executor = Substitute.For<IAIGenerationOperationExecutor>();
        executor.OperationType.Returns(AIGenerationOperations.ApplicationAnalysis);
        using var serviceProvider = new ServiceCollection()
            .AddSingleton(executor)
            .BuildServiceProvider();
        var registry = new AIGenerationOperationExecutorRegistry(serviceProvider);

        registry.Resolve(AIGenerationOperations.ApplicationAnalysis).ShouldBeSameAs(executor);
    }

    [Theory]
    [InlineData(AIGenerationOperations.AttachmentSummary)]
    [InlineData(AIGenerationOperations.ApplicationAnalysis)]
    [InlineData(AIGenerationOperations.ApplicationScoring)]
    [InlineData(AIGenerationOperations.FormMapping)]
    [InlineData(AIGenerationOperations.FormScoresheet)]
    [InlineData(AIGenerationOperations.FormWorksheet)]
    public void Resolve_Should_Return_Each_Registered_Operation(string operationType)
    {
        var executors = AIGenerationOperations.All
            .Select(operation =>
            {
                var executor = Substitute.For<IAIGenerationOperationExecutor>();
                executor.OperationType.Returns(operation.OperationType);
                return executor;
            })
            .ToArray();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IAIGenerationOperationExecutor>(sp => executors.Single(x =>
                x.OperationType == operationType))
            .BuildServiceProvider();
        var registry = new AIGenerationOperationExecutorRegistry(serviceProvider);

        registry.Resolve(operationType).OperationType.ShouldBe(operationType);
    }

    [Fact]
    public void Resolve_Should_Reject_Unregistered_Operation_Type()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var registry = new AIGenerationOperationExecutorRegistry(serviceProvider);

        Should.Throw<UserFriendlyException>(() => registry.Resolve(AIGenerationOperations.FormMapping));
    }
}
