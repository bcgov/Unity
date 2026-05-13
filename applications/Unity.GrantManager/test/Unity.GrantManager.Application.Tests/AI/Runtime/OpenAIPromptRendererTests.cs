using Shouldly;
using System;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIPromptRendererTests
{
    [Theory]
    [InlineData("v0")]
    [InlineData("v1")]
    [InlineData(" v1 ")]
    public void ResolvePromptVersion_Should_Return_Supported_Version(string version)
    {
        OpenAIPromptRenderer.ResolvePromptVersion(version)
            .ShouldBe(version.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("V1")]
    [InlineData("vNext")]
    public void ResolvePromptVersion_Should_Throw_When_Version_Is_Missing_Or_Unsupported(string? version)
    {
        Should.Throw<InvalidOperationException>(() => OpenAIPromptRenderer.ResolvePromptVersion(version));
    }
}
