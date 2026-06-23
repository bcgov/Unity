using Shouldly;
using System.Text.Json;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIProviderPayloadValidatorTests
{
    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_For_InvalidJson()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson("not-json");

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("not valid JSON");
    }

    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_When_Decision_Is_Missing()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson(
            """
            {
              "errors": [],
              "warnings": [],
              "summaries": [],
              "recommendations": []
            }
            """);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("decision");
    }

    [Fact]
    public void ValidateApplicationScoringJson_Should_Return_InvalidOutput_When_Answer_Is_Missing()
    {
        var sectionJson = JsonSerializer.Serialize(new[]
        {
            new { id = "q1" }
        });

        var result = AIProviderPayloadValidator.ValidateApplicationScoringJson("{}", sectionJson);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("q1");
    }

    [Fact]
    public void ValidateAttachmentSummaryText_Should_Return_InvalidOutput_For_Empty_Text()
    {
        var result = AIProviderPayloadValidator.ValidateAttachmentSummaryText(string.Empty);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("empty");
    }
}
