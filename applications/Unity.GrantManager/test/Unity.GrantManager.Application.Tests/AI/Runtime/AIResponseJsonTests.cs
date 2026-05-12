using Shouldly;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIResponseJsonTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void CleanJsonResponse_Should_ReturnEmpty_ForNullOrWhitespace(string? input, string expected)
    {
        AIResponseJson.CleanJsonResponse(input!).ShouldBe(expected);
    }

    [Fact]
    public void CleanJsonResponse_Should_StripMarkdownJsonFence_WithNewline()
    {
        var input = "```json\n{\"key\":\"value\"}\n```";
        AIResponseJson.CleanJsonResponse(input).ShouldBe("{\"key\":\"value\"}");
    }

    [Fact]
    public void CleanJsonResponse_Should_StripPlainFence_WithNewline()
    {
        var input = "```\n{\"key\":\"value\"}\n```";
        AIResponseJson.CleanJsonResponse(input).ShouldBe("{\"key\":\"value\"}");
    }

    [Fact]
    public void CleanJsonResponse_Should_StripTrailingFence_OnlyAfterContent()
    {
        var input = "{\"key\":\"value\"}```";
        AIResponseJson.CleanJsonResponse(input).ShouldBe("{\"key\":\"value\"}");
    }

    [Fact]
    public void CleanJsonResponse_Should_ReturnTrimmedJson_WithNoFences()
    {
        var input = "  {\"key\":\"value\"}  ";
        AIResponseJson.CleanJsonResponse(input).ShouldBe("{\"key\":\"value\"}");
    }

    [Fact]
    public void CleanJsonResponse_Should_HandleFenceWithoutNewline_UsingFirstJsonToken()
    {
        var input = "```json{\"key\":\"value\"}```";
        var result = AIResponseJson.CleanJsonResponse(input);
        result.ShouldBe("{\"key\":\"value\"}");
    }
}
