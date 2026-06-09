using Shouldly;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIOperationResultTests
{
    [Fact]
    public void Failure_Factories_Should_Set_Consistent_Failure_Categories()
    {
        AIOperationResult.Success().FailureCategory.ShouldBe(AIFailureCategory.None);
        AIOperationResult.ProviderUnavailable().FailureCategory.ShouldBe(AIFailureCategory.ProviderUnavailable);
        AIOperationResult.TransientFailure().FailureCategory.ShouldBe(AIFailureCategory.TransientProviderFailure);
        AIOperationResult.PermanentFailure().FailureCategory.ShouldBe(AIFailureCategory.PermanentProviderFailure);
        AIOperationResult.InvalidOutput().FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
    }

    [Fact]
    public void WithOutcome_Should_Resolve_Category_When_Not_Explicit()
    {
        var result = AIOperationResult.Success().WithOutcome(AIOperationOutcome.InvalidOutput);

        result.Outcome.ShouldBe(AIOperationOutcome.InvalidOutput);
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
    }
}
