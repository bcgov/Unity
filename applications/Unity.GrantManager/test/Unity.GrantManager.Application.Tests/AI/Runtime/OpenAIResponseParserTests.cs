using Shouldly;
using System.Collections.Generic;
using System.Text.Json;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIResponseParserTests
{
    [Fact]
    public void ParseApplicationScoringResponse_Should_Preserve_All_Answer_Keys_From_Section_Output()
    {
        var raw = """
        {
          "q1": {
            "answer": "No",
            "rationale": "There is no direct evidence in DATA or ATTACHMENTS that the lead applicant meets any stated program eligibility criteria; eligibility is not explicitly confirmed.",
            "confidence": 0.2
          },
          "q2": {
            "answer": "2",
            "rationale": "Although a facility partner (Riverbend Rec Center) is mentioned in CustomField3, there is no direct evidence that any project partner is explicitly eligible under program rules, so eligibility cannot be confirmed.",
            "confidence": 0.2
          },
          "q3": {
            "answer": "No",
            "rationale": "The application materials describe a Community Kitchen Expansion but there is no statement or evidence showing this specific project is within the program scope, so scope compliance cannot be confirmed.",
            "confidence": 0.2
          },
          "q4": {
            "answer": "No",
            "rationale": "Project location details (Riverbend, BC) are provided, but there is no evidence confirming the location meets any program-specific eligibility requirements, so location eligibility is not demonstrated.",
            "confidence": 0.2
          },
          "q5": {
            "answer": "2",
            "rationale": "The submission does not explicitly identify an appropriate owner for the kitchen infrastructure in the provided DATA or ATTACHMENTS, and the Project Risk and Feasibility section is not present to confirm ownership.",
            "confidence": 0.2
          },
          "q6": {
            "answer": "No",
            "rationale": "The set of attachments is provided, but there is no checklist or statement of mandatory supporting documents in DATA or ATTACHMENTS to confirm all required documents are included.",
            "confidence": 0.2
          },
          "q7": {
            "answer": "The submission lacks direct, explicit evidence for eligibility, scope, location, ownership, and required documents.",
            "rationale": "DATA shows organizational and project descriptions and several inconsistent attachment profiles, but none directly confirm lead applicant eligibility, partner eligibility, program scope fit, site eligibility, infrastructure ownership, or inclusion of mandatory supporting documents.",
            "confidence": 0.2
          }
        }
        """;

        var result = OpenAIResponseParser.ParseApplicationScoringResponse(raw);

        result.Answers.Count.ShouldBe(7);
        result.Answers.Keys.ShouldBe(new[]
        {
            "q1",
            "q2",
            "q3",
            "q4",
            "q5",
            "q6",
            "q7"
        });

        result.Answers["q1"].Answer.GetString().ShouldBe("No");
        result.Answers["q7"].Answer.GetString().ShouldBe("The submission lacks direct, explicit evidence for eligibility, scope, location, ownership, and required documents.");
    }

    [Fact]
    public void ParseApplicationScoringResponse_Should_Ignore_Non_Object_Properties()
    {
        var raw = """
        {
          "q1": {
            "answer": "Yes",
            "rationale": "ok",
            "confidence": 0.9
          },
          "note": "should be ignored"
        }
        """;

        var result = OpenAIResponseParser.ParseApplicationScoringResponse(raw);

        result.Answers.Count.ShouldBe(1);
        result.Answers.ShouldContainKey("q1");
        result.Answers.ShouldNotContainKey("note");
    }

    [Fact]
    public void ParseApplicationScoringResponse_Should_Parse_Mixed_Answer_Types_From_Project_Need_Output()
    {
        var raw = """
        {
          "q1": {
            "answer": 0,
            "rationale": "No CAT score is present in the provided DATA or any ATTACHMENTS. There is no direct evidence of a community CAT value for Riverbend, so the most conservative numeric response is 0.",
            "confidence": 0.2
          },
          "q2": {
            "answer": "4",
            "rationale": "The ProjectSummary in DATA explicitly identifies expanding the community kitchen to support meal programs, volunteer training, and food security initiatives, which indicates multiple community needs with moderate detail. However, there is no supporting data, needs assessment, or quantitative evidence, so a score reflecting moderate information for multiple needs is appropriate.",
            "confidence": 0.6
          },
          "q3": {
            "answer": "The submission names food security, meal programs, and volunteer training as community needs but provides only brief descriptive information without supporting data, needs assessments, or stakeholder consultation evidence.",
            "rationale": "Evidence is limited to the one-line ProjectSummary in DATA stating the project purpose. Attachments describe different mock projects and do not provide supporting documentation for this kitchen project, so the identified needs are described but not substantiated.",
            "confidence": 0.6
          }
        }
        """;

        var result = OpenAIResponseParser.ParseApplicationScoringResponse(raw);

        result.Answers.Count.ShouldBe(3);
        result.Answers.ShouldContainKey("q1");
        result.Answers.ShouldContainKey("q2");
        result.Answers.ShouldContainKey("q3");
        result.Answers["q1"].Answer.GetInt32().ShouldBe(0);
        result.Answers["q2"].Answer.GetString().ShouldBe("4");
        var q3Answer = result.Answers["q3"].Answer.GetString();
        q3Answer.ShouldNotBeNull();
        q3Answer.ShouldContain("food security");
    }

    [Fact]
    public void ParseApplicationScoringResponse_Should_Round_Decimal_Confidence_To_Nearest_Ten()
    {
        var raw = """
        {
          "q1": {
            "answer": "No",
            "rationale": "The record does not directly confirm the condition.",
            "confidence": 0.33
          },
          "q2": {
            "answer": "No",
            "rationale": "The record does not directly confirm the condition.",
            "confidence": 0.86
          }
        }
        """;

        var result = OpenAIResponseParser.ParseApplicationScoringResponse(raw);

        result.Answers["q1"].Confidence.ShouldBe(30);
        result.Answers["q2"].Confidence.ShouldBe(90);
    }

    [Fact]
    public void ParseAttachmentSummaryBatchResponse_Should_Map_Attachment_Ids_To_Summaries()
    {
        var raw = """
        {
          "attachments": [
            { "attachmentId": "a1", "summary": "One" },
            { "attachmentId": "a2", "summary": "Two" }
          ]
        }
        """;

        var result = OpenAIResponseParser.ParseAttachmentSummaryBatchResponse(raw);

        result.Attachments.Count.ShouldBe(2);
        result.Attachments[0].AttachmentId.ShouldBe("a1");
        result.Attachments[0].Summary.ShouldBe("One");
        result.Attachments[1].AttachmentId.ShouldBe("a2");
        result.Attachments[1].Summary.ShouldBe("Two");
    }
}
