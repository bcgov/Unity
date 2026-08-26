using Shouldly;
using System;
using System.Text.Json;
using Unity.AI.Runtime.Execution;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class PromptResponseValidatorTests
{
    [Fact]
    public void ValidateApplicationAnalysisJson_Should_Return_InvalidOutput_For_InvalidJson()
    {
        var result = AIProviderPayloadValidator.ValidateApplicationAnalysisJson("not-json");

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
        var reason = result.Reason;
        reason.ShouldNotBeNull();
        reason.ShouldContain("not valid JSON");
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
        var reason = result.Reason;
        reason.ShouldNotBeNull();
        reason.ShouldContain("decision");
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
        result.Reason!.ShouldContain("q1");
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
        result.Reason!.ShouldContain("Expected 'PROCEED' or 'HOLD'");
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
        var reason = result.Reason;
        reason.ShouldNotBeNull();
        reason.ShouldContain("summaries");
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
        var reason = result.Reason;
        reason.ShouldNotBeNull();
        reason.ShouldContain("recommendations");
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
        var reason = result.Reason;
        reason.ShouldNotBeNull();
        reason.ShouldContain("empty");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"title":"Draft","sections":[]}""")]
    public void ValidateFormWorksheetJson_Should_Return_InvalidOutput_For_Incomplete_Worksheet(string response)
    {
        var result = AIProviderPayloadValidator.ValidateFormWorksheetJson(response);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
    }

    [Fact]
    public void ValidateFormWorksheetJson_Should_Return_Success_For_Flat_Field_Suggestions()
    {
        var result = AIProviderPayloadValidator.ValidateFormWorksheetJson(
            """
            {
              "fields": [
                { "key": "projectName", "label": "Project Name", "type": "Text" }
              ]
            }
            """);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateFormScoresheetJson_Should_Return_Success_For_Complete_Scoresheet()
    {
        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(ValidFormScoresheetJson);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, "{\"min\":0,\"max\":10}")]
    [InlineData(2, "{\"minLength\":0,\"maxLength\":100}")]
    [InlineData(6, "{\"yes_value\":1,\"no_value\":0}")]
    [InlineData(12, "{\"options\":[{\"key\":\"yes\",\"value\":\"Yes\",\"numeric_value\":1}]}")]
    [InlineData(14, "{\"minLength\":0,\"maxLength\":100,\"rows\":3}")]
    public void ValidateFormScoresheetJson_Should_Return_Success_For_Valid_Field_Definitions(int type, string definition)
    {
        var response = ValidFormScoresheetJson
            .Replace("\"Type\": 1", $"\"Type\": {type}", StringComparison.Ordinal)
            .Replace("\"Definition\": \"{\\\"min\\\": 0, \\\"max\\\": 10}\"", $"\"Definition\": \"{definition.Replace("\"", "\\\"", StringComparison.Ordinal)}\"", StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("{\"min\":0}")]
    [InlineData("{\"min\":10,\"max\":1}")]
    [InlineData("{\"minLength\":0,\"maxLength\":-1}")]
    [InlineData("{\"minLength\":0,\"maxLength\":10,\"rows\":0}")]
    public void ValidateFormScoresheetJson_Should_Return_InvalidOutput_For_Invalid_Number_Definition(string definition)
    {
        var response = ValidFormScoresheetJson
            .Replace("\"Definition\": \"{\\\"min\\\": 0, \\\"max\\\": 10}\"", $"\"Definition\": \"{definition.Replace("\"", "\\\"", StringComparison.Ordinal)}\"", StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
    }

    [Fact]
    public void ValidateFormScoresheetJson_Should_Allow_Empty_Optional_Reporting_Fields()
    {
        var response = ValidFormScoresheetJson
            .Replace("\"ReportColumns\": \"score\"", "\"ReportColumns\": \"\"", StringComparison.Ordinal)
            .Replace("\"ReportKeys\": \"project_score\"", "\"ReportKeys\": \"\"", StringComparison.Ordinal)
            .Replace("\"ReportViewName\": \"scoresheet_report\"", "\"ReportViewName\": \"\"", StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateFormScoresheetJson_Should_Allow_Mixed_Property_Casing()
    {
        var response = ValidFormScoresheetJson
            .Replace("\"Title\"", "\"title\"", StringComparison.Ordinal)
            .Replace("\"Name\"", "\"name\"", StringComparison.Ordinal)
            .Replace("\"Version\"", "\"version\"", StringComparison.Ordinal)
            .Replace("\"Order\"", "\"order\"", StringComparison.Ordinal)
            .Replace("\"Published\"", "\"published\"", StringComparison.Ordinal)
            .Replace("\"Sections\"", "\"sections\"", StringComparison.Ordinal)
            .Replace("\"Fields\"", "\"fields\"", StringComparison.Ordinal)
            .Replace("\"ReportColumns\"", "\"reportColumns\"", StringComparison.Ordinal)
            .Replace("\"ReportKeys\"", "\"reportKeys\"", StringComparison.Ordinal)
            .Replace("\"ReportViewName\"", "\"reportViewName\"", StringComparison.Ordinal)
            .Replace("\"Description\"", "\"description\"", StringComparison.Ordinal)
            .Replace("\"Type\"", "\"type\"", StringComparison.Ordinal)
            .Replace("\"Definition\"", "\"definition\"", StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("\"Title\": \"Generated scoresheet\"", "\"Title\": \"\"")]
    [InlineData("\"Version\": 1", "\"Version\": \"1\"")]
    [InlineData("\"Published\": true", "\"Published\": \"true\"")]
    [InlineData("\"Definition\": \"{\\\"min\\\": 0, \\\"max\\\": 10}\"", "\"Definition\": \"not-json\"")]
    public void ValidateFormScoresheetJson_Should_Return_InvalidOutput_For_Invalid_Required_Value(string validValue, string invalidValue)
    {
        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(ValidFormScoresheetJson.Replace(validValue, invalidValue, StringComparison.Ordinal));

        result.IsValid.ShouldBeFalse();
        result.FailureCategory.ShouldBe(AIFailureCategory.InvalidOutput);
    }

    [Fact]
    public void ValidateFormScoresheetJson_Should_Return_InvalidOutput_For_Duplicate_Question_Names()
    {
        var response = ValidFormScoresheetJson.Replace(
            "\"Fields\": [",
            "\"Fields\": [{ \"Name\": \"project_score\", \"Label\": \"Duplicate\", \"Order\": 1, \"Type\": 1, \"Definition\": \"{\\\"min\\\":0,\\\"max\\\":10}\" },",
            StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeFalse();
        result.Reason!.ShouldContain("duplicate field names");
    }

    [Fact]
    public void ValidateFormScoresheetJson_Should_Return_InvalidOutput_For_Duplicate_Section_Names()
    {
        var response = ValidFormScoresheetJson.Replace(
            "\"Sections\": [",
            "\"Sections\": [{ \"Name\": \"Review\", \"Order\": 1, \"Fields\": [{ \"Name\": \"second_score\", \"Label\": \"Second score\", \"Order\": 0, \"Type\": 1, \"Definition\": \"{\\\"min\\\":0,\\\"max\\\":10}\" }] },",
            StringComparison.Ordinal);

        var result = AIProviderPayloadValidator.ValidateFormScoresheetJson(response);

        result.IsValid.ShouldBeFalse();
        result.Reason!.ShouldContain("duplicate section names");
    }

    private const string ValidFormScoresheetJson = """
        {
          "Title": "Generated scoresheet",
          "Name": "generated-scoresheet",
          "Version": 1,
          "Order": 0,
          "Published": true,
          "ReportColumns": "score",
          "ReportKeys": "project_score",
          "ReportViewName": "scoresheet_report",
          "Sections": [
            {
              "Name": "Review",
              "Order": 0,
              "Fields": [
                {
                  "Name": "project_score",
                  "Label": "Project score",
                  "Order": 0,
                  "Type": 1,
                  "Definition": "{\"min\": 0, \"max\": 10}"
                }
              ]
            }
          ]
        }
        """;

}
