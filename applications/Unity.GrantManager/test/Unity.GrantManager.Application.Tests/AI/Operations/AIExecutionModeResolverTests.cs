using Microsoft.Extensions.Configuration;
using Shouldly;
using System.Collections.Generic;
using Unity.AI.Operations;
using Unity.AI.Prompts;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AIExecutionModeResolverTests
{
    [Fact]
    public void ResolveMode_Uses_Operation_Override_Before_Default()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:ExecutionMode"] = "Parallel",
                [$"Azure:Operations:{AIPromptTypes.ApplicationScoring}:ExecutionMode"] = "Batch"
            })
            .Build();

        var resolver = new AIExecutionModeResolver(configuration);

        resolver.ResolveMode(AIPromptTypes.ApplicationScoring).ShouldBe(AIExecutionMode.Batch);
        resolver.ResolveMode(AIPromptTypes.AttachmentSummary).ShouldBe(AIExecutionMode.Parallel);
    }
}
