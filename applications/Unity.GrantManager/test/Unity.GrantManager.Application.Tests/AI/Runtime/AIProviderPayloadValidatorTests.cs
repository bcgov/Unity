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
    public void ValidateApplicationScoringJson_Should_Return_Success_For_Decimal_Confidence()
    {
        var sectionJson = JsonSerializer.Serialize(new[]
        {
            new { id = "q1" }
        });

        var result = AIProviderPayloadValidator.ValidateApplicationScoringJson(
            """
            {
              "q1": {
                "answer": "No",
                "rationale": "The record does not directly confirm the condition.",
                "confidence": 0.30
              }
            }
            """,
            sectionJson);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_When_Decision_Is_Not_Proceed_Or_Hold()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson(
            """
            {
              "decision": "unknown",
              "errors": [],
              "warnings": [],
              "summaries": [
                { "title": "Summary", "detail": "Content" }
              ],
              "recommendations": []
            }
            """);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("Expected 'PROCEED' or 'HOLD'");
    }

    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_When_All_Findings_Are_Empty()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson(
            """
            {
              "decision": "PROCEED",
              "errors": [],
              "warnings": [],
              "summaries": [],
              "recommendations": []
            }
            """);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("summaries");
    }

    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_When_Recommendations_Are_Empty()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson(
            """
            {
              "decision": "PROCEED",
              "errors": [],
              "warnings": [],
              "summaries": [
                { "title": "Summary", "detail": "Looks complete." }
              ],
              "recommendations": []
            }
            """);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        result.Reason.ShouldContain("recommendations");
    }

    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_Success_For_Proceed_With_Findings()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson(
            """
            {
              "decision": "PROCEED",
              "errors": [],
              "warnings": [],
              "summaries": [
                { "title": "Summary", "detail": "Looks complete." }
              ],
              "recommendations": [
                { "title": "Proceed", "detail": "No blocking issues remain." }
              ]
            }
            """);

        result.IsValid.ShouldBeTrue();
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
