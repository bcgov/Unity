using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
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

    [Fact]
    public void BuildApplicationScoringUserPrompt_Should_Render_Structured_Empty_Attachments()
    {
        var prompt = OpenAIPromptRenderer.BuildApplicationScoringUserPrompt(
            "v1",
            "{}",
            "[]",
            "{\"name\":\"Test\",\"questions\":[{\"id\":\"q1\",\"type\":\"YesNo\"}]}",
            "{}");

        var normalized = prompt.Replace("\r\n", "\n");

        normalized.ShouldContain("ATTACHMENTS\n[]\n\nSECTION");
        normalized.ShouldContain("If ATTACHMENTS is empty, use DATA only");
        normalized.ShouldNotContain("No attachments provided.");
    }

    [Fact]
    public void BuildApplicationAnalysisUserPrompt_Should_Not_Treat_FormIo_Mustache_Data_As_Prompt_Placeholders()
    {
        const string data = """
            {
              "template": "<span>{{ item.label }}</span>",
              "fileNameTemplate": "{{fileName}}",
              "sector": "{{ item.SectorName }}",
              "topicSourceId": "{{ item.topic_source_id }}",
              "uppercaseTemplateValue": "{{FORMIO_VALUE}}"
            }
            """;

        var result = OpenAIPromptRenderer.BuildApplicationAnalysisUserPrompt(
            "v1",
            "{}",
            data,
            "[]");

        result.ShouldContain("{{ item.label }}");
        result.ShouldContain("{{fileName}}");
        result.ShouldContain("{{ item.SectorName }}");
        result.ShouldContain("{{ item.topic_source_id }}");
        result.ShouldContain("{{FORMIO_VALUE}}");
    }

    [Fact]
    public void RenderPromptTemplate_Should_Reject_Non_Prompt_Placeholders_In_Template_Text()
    {
        var templateCache = GetPromptTemplateCache();
        templateCache["v1/unit-test.invalid"] = "Bad template token: {{ item.label }}";

        var exception = Should.Throw<TargetInvocationException>(() =>
            RenderPromptTemplate("v1", "unit-test.invalid", new Dictionary<string, string>()));

        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        exception.InnerException!.Message.ShouldContain("Invalid prompt placeholders");
        exception.InnerException.Message.ShouldContain("item.label");
    }

    private static string RenderPromptTemplate(
        string version,
        string templateName,
        IReadOnlyDictionary<string, string> runtimeReplacements)
    {
        var method = typeof(OpenAIPromptRenderer).GetMethod(
            "RenderPromptTemplate",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        return (string)method.Invoke(null, [version, templateName, runtimeReplacements])!;
    }

    private static ConcurrentDictionary<string, string> GetPromptTemplateCache()
    {
        var field = typeof(OpenAIPromptRenderer).GetField(
            "PromptTemplateCache",
            BindingFlags.Static | BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        return (ConcurrentDictionary<string, string>)field.GetValue(null)!;
    }
}
