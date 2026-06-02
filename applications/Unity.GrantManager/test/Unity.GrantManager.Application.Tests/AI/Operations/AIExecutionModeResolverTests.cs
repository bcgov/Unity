using Microsoft.Extensions.Configuration;
using Shouldly;
using System;
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

    [Fact]
    public void ResolveMode_Should_Throw_When_Default_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var resolver = new AIExecutionModeResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveMode(AIPromptTypes.AttachmentSummary));
        ex.Message.ShouldContain(AIPromptTypes.AttachmentSummary);
    }

    [Fact]
    public void ResolveMode_Should_Throw_When_Configured_Value_Is_Invalid()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:ExecutionMode"] = "Fast"
            })
            .Build();

        var resolver = new AIExecutionModeResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveMode(AIPromptTypes.AttachmentSummary));
        ex.Message.ShouldContain(AIPromptTypes.AttachmentSummary);
    }
}
