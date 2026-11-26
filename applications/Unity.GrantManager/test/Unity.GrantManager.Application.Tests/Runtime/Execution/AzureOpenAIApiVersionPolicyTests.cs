using Shouldly;
using System;
using Unity.AI.Runtime.Execution;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AzureOpenAIApiVersionPolicyTests
{
    [Fact]
    public void Should_Replace_The_SDK_Api_Version_With_The_Models()
    {
        var uri = new Uri("https://proxy.test/openai/deployments/gpt-5-mini/chat/completions?api-version=2024-10-21");

        var result = AzureOpenAIApiVersionPolicy.WithApiVersion(uri, "2025-04-01-preview");

        result.ToString().ShouldBe(
            "https://proxy.test/openai/deployments/gpt-5-mini/chat/completions?api-version=2025-04-01-preview");
    }

    [Fact]
    public void Should_Add_Api_Version_And_Keep_Other_Parameters()
    {
        var uri = new Uri("https://proxy.test/openai/deployments/text-embedding-3-large/embeddings?x=1");

        var result = AzureOpenAIApiVersionPolicy.WithApiVersion(uri, "2024-10-21");

        result.Query.ShouldBe("?x=1&api-version=2024-10-21");
    }

    [Theory]
    [InlineData("2024-10-21", true)]
    [InlineData("2025-04-01-preview", true)]
    [InlineData(" 2024-06-01 ", true)]
    [InlineData("latest", false)]
    [InlineData("2024-10-21&x=1", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Should_Accept_Only_Well_Formed_Api_Versions(string? apiVersion, bool expected)
    {
        AzureOpenAIApiVersions.IsValid(apiVersion).ShouldBe(expected);
    }
}
