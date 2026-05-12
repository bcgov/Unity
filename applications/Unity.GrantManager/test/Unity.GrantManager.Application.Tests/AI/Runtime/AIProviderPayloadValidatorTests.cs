using Shouldly;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIProviderPayloadValidatorTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Some summary text", true)]
    public void IsValidAttachmentSummaryText_Should_RejectBlankAndAcceptContent(string? input, bool expected)
    {
        AIProviderPayloadValidator.IsValidAttachmentSummaryText(input!).ShouldBe(expected);
    }

    [Fact]
    public void IsValidApplicationAnalysisJson_Should_ReturnTrue_ForWellFormedPayload()
    {
        var json = """
            {
              "decision": "Approved",
              "errors": [],
              "warnings": [],
              "summaries": [],
              "recommendations": []
            }
            """;
        AIProviderPayloadValidator.IsValidApplicationAnalysisJson(json).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void IsValidApplicationAnalysisJson_Should_ReturnFalse_ForInvalidInput(string? input)
    {
        AIProviderPayloadValidator.IsValidApplicationAnalysisJson(input!).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationAnalysisJson_Should_ReturnFalse_WhenDecisionMissing()
    {
        var json = """{"errors":[],"warnings":[],"summaries":[],"recommendations":[]}""";
        AIProviderPayloadValidator.IsValidApplicationAnalysisJson(json).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationAnalysisJson_Should_ReturnFalse_WhenErrorsIsNotArray()
    {
        var json = """{"decision":"ok","errors":"bad","warnings":[],"summaries":[],"recommendations":[]}""";
        AIProviderPayloadValidator.IsValidApplicationAnalysisJson(json).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationAnalysisJson_Should_AcceptMarkdownWrappedJson()
    {
        var json = "```json\n{\"decision\":\"ok\",\"errors\":[],\"warnings\":[],\"summaries\":[],\"recommendations\":[]}\n```";
        AIProviderPayloadValidator.IsValidApplicationAnalysisJson(json).ShouldBeTrue();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnTrue_ForWellFormedPayload()
    {
        var sectionJson = """[{"id":"q1"},{"id":"q2"}]""";
        var response = """
            {
              "q1": {"answer": "Yes", "confidence": 85},
              "q2": {"answer": "No",  "confidence": 42}
            }
            """;
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeTrue();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnFalse_WhenSectionJsonIsEmpty()
    {
        AIProviderPayloadValidator.IsValidApplicationScoringJson("{}", "[]").ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnFalse_WhenAnswerMissing()
    {
        var sectionJson = """[{"id":"q1"}]""";
        var response = """{"q1": {"confidence": 50}}""";
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnFalse_WhenConfidenceOutOfRange()
    {
        var sectionJson = """[{"id":"q1"}]""";
        var response = """{"q1": {"answer": "Yes", "confidence": 150}}""";
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnFalse_WhenQuestionMissingFromResponse()
    {
        var sectionJson = """[{"id":"q1"},{"id":"q2"}]""";
        var response = """{"q1": {"answer": "Yes", "confidence": 80}}""";
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeFalse();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_AcceptQuestionsWrappedInObject()
    {
        var sectionJson = """{"questions":[{"id":"q1"}]}""";
        var response = """{"q1": {"answer": "Yes", "confidence": 75}}""";
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeTrue();
    }

    [Fact]
    public void IsValidApplicationScoringJson_Should_ReturnFalse_WhenConfidenceIsNegative()
    {
        var sectionJson = """[{"id":"q1"}]""";
        var response = """{"q1": {"answer": "Yes", "confidence": -1}}""";
        AIProviderPayloadValidator.IsValidApplicationScoringJson(response, sectionJson).ShouldBeFalse();
    }
}
