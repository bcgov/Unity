using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AIExecutionStrategyTests
{
    [Fact]
    public async Task Batch_Uses_Single_Batch_Operation()
    {
        var itemCalls = 0;
        var batchCalls = 0;
        IReadOnlyCollection<int>? batchItems = null;

        var results = await AIExecutionStrategy.RunAsync(
            [1, 2, 3],
            AIExecutionMode.Batch,
            item =>
            {
                itemCalls++;
                return Task.FromResult(item);
            },
            items =>
            {
                batchCalls++;
                batchItems = items;
                return Task.FromResult(new List<int> { 6 });
            });

        itemCalls.ShouldBe(0);
        batchCalls.ShouldBe(1);
        batchItems.ShouldNotBeNull();
        batchItems.Count.ShouldBe(3);
        results.ShouldBe([6]);
    }
}
