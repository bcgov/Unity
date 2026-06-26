using Shouldly;
using System;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIPromptTemplateRendererTests
{
    [Fact]
    public void BuildApplicationAnalysisUserPrompt_Should_Render_Metadata_And_Preserve_FormIo_Data()
    {
        const string template = """
            SCHEMA
            {{SCHEMA}}

            DATA
            {{DATA}}

            ATTACHMENTS
            {{ATTACHMENTS}}

            RULES
            {{RULES}}
            {{COMMON_RULES}}
            """;

        const string metadataJson = """
            {
              "RULES": "- Use only provided input sections as evidence.",
              "COMMON_RULES": "- If ATTACHMENTS is empty, use DATA only and do not mention missing attachments unless their absence is material."
            }
            """;

        const string data = """
            {
              "template": "<span>{{ item.label }}</span>",
              "fileNameTemplate": "{{fileName}}",
              "uppercaseTemplateValue": "{{FORMIO_VALUE}}"
            }
            """;

        var result = AIPromptTemplateRenderer.BuildApplicationAnalysisUserPrompt(
            template,
            "{}",
            data,
            "[]",
            metadataJson);

        result.ShouldContain("{{ item.label }}");
        result.ShouldContain("{{fileName}}");
        result.ShouldContain("{{FORMIO_VALUE}}");
        result.ShouldContain("If ATTACHMENTS is empty, use DATA only");
        result.ShouldContain("Use only provided input sections as evidence.");
    }

    [Fact]
    public void BuildApplicationScoringUserPrompt_Should_Render_Metadata_Sections()
    {
        const string template = """
            DATA
            {{DATA}}

            ATTACHMENTS
            {{ATTACHMENTS}}

            SECTION
            {{SECTION}}

            RESPONSE
            {{RESPONSE}}

            RULES
            {{RULES}}
            {{COMMON_RULES}}
            """;

        const string metadataJson = """
            {
              "RULES": "- Use only DATA and ATTACHMENTS as evidence.",
              "COMMON_RULES": "- Return valid JSON only."
            }
            """;

        var result = AIPromptTemplateRenderer.BuildApplicationScoringUserPrompt(
            template,
            "{}",
            "[]",
            "{\"name\":\"Test\",\"questions\":[]}",
            "{}",
            metadataJson);

        result.ShouldContain("Use only DATA and ATTACHMENTS as evidence.");
        result.ShouldContain("Return valid JSON only.");
    }

    [Fact]
    public void BuildApplicationAnalysisUserPrompt_Should_Map_Output_Metadata_To_Response_Placeholder()
    {
        const string template = """
            RESPONSE
            {{RESPONSE}}
            """;

        const string metadataJson = """
            {
              "OUTPUT": "{ \"decision\": \"PROCEED\" }"
            }
            """;

        var result = AIPromptTemplateRenderer.BuildApplicationAnalysisUserPrompt(
            template,
            "{}",
            "{}",
            "[]",
            metadataJson);

        result.ShouldContain("{ \"decision\": \"PROCEED\" }");
    }

    [Fact]
    public void BuildApplicationAnalysisUserPrompt_Should_Throw_When_Template_Contains_NonPrompt_Placeholders()
    {
        const string template = "Bad template token: {{ item.label }}";

        var exception = Should.Throw<InvalidOperationException>(() =>
            AIPromptTemplateRenderer.BuildApplicationAnalysisUserPrompt(template, "{}", "{}", "[]"));

        exception.Message.ShouldContain("Invalid prompt placeholders");
        exception.Message.ShouldContain("item.label");
    }

    [Fact]
    public void BuildApplicationAnalysisUserPrompt_Should_Throw_When_Metadata_Is_Invalid_Json()
    {
        const string template = "{{DATA}}";
        const string invalidMetadataJson = "{ invalid json";

        var exception = Should.Throw<InvalidOperationException>(() =>
            AIPromptTemplateRenderer.BuildApplicationAnalysisUserPrompt(
                template,
                "{}",
                "{}",
                "[]",
                invalidMetadataJson));

        exception.Message.ShouldContain("Invalid prompt metadata JSON.");
    }
}
