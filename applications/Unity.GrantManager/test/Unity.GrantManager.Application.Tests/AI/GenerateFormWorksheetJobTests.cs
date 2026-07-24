using System;
using System.Linq;
using Shouldly;
using Unity.Flex.Worksheets;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Xunit;

namespace Unity.GrantManager.AI;

public class GenerateFormWorksheetJobTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"fields":[{"key":"","label":"Project","type":"Text"}]}""")]
    [InlineData("""{"fields":[{"key":"project","label":"","type":"Text"}]}""")]
    [InlineData("""{"fields":[{"key":"project","label":"Project","type":"Radio"}]}""")]
    [InlineData("""{"fields":[{"key":"project","label":"Project","type":2}]}""")]
    [InlineData("""{"fields":[{"key":"project","label":"Project","type":"Text"},{"key":"PROJECT","label":"Other","type":"Text"}]}""")]
    public void ParseWorksheetDefinition_Should_Reject_Incomplete_Ai_Response(string worksheetJson)
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            GenerateFormWorksheetJob.ParseWorksheetDefinition(worksheetJson));

        exception.Message.ShouldContain("unusable worksheet definition");
    }

    [Fact]
    public void ParseWorksheetDefinition_Should_Accept_Flat_Safe_Field_Suggestions()
    {
        var fields = GenerateFormWorksheetJob.ParseWorksheetDefinition("""
            {"fields":[{"key":"projectName","label":"Project Name","type":"Text"},{"key":"requestedAmount","label":"Requested Amount","type":"Currency"}]}
            """);

        fields.Count.ShouldBe(2);
        fields[0].Key.ShouldBe("projectName");
        fields[0].ResolvedType.ShouldBe(CustomFieldType.Text);
        fields[1].ResolvedType.ShouldBe(CustomFieldType.Currency);
    }

    [Fact]
    public void BuildWorksheet_Should_Create_One_SuggestedFields_Section_With_Default_Definitions()
    {
        var suggestions = GenerateFormWorksheetJob.ParseWorksheetDefinition("""
            {"fields":[{"key":"projectName","label":"Project Name","type":"Text"}]}
            """);

        var worksheet = GenerateFormWorksheetJob.BuildWorksheet(suggestions, "ai-form-worksheet");

        worksheet.Sections.Count.ShouldBe(1);
        worksheet.Sections.Single().Name.ShouldBe("Suggested Fields");
        var field = worksheet.Sections.Single().Fields.Single();
        field.Order.ShouldBe(1u);
        field.Definition.ShouldContain("maxLength");
    }
}
