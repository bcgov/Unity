using Shouldly;
using Xunit;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class AiDraftNameTests
{
    [Theory]
    [InlineData("Risk Review", "riskreview")]
    [InlineData("  RISK---Review!  ", "riskreview")]
    [InlineData("École Grant", "écolegrant")]
    public void NormalizeTitle_Should_Remove_Whitespace_And_Punctuation(string title, string expected)
    {
        AiDraftName.NormalizeTitle(title).ShouldBe(expected);
    }

    [Fact]
    public void BuildBaseName_Should_Prefix_Normalized_Title()
    {
        AiDraftName.BuildBaseName("Risk Review").ShouldBe("ai-riskreview");
    }

    [Fact]
    public void BuildBaseName_Should_Use_Draft_When_Title_Has_No_Alphanumeric_Characters()
    {
        AiDraftName.BuildBaseName("!!!").ShouldBe("ai-draft");
    }

    [Fact]
    public void Suggestion_Names_Should_Use_Only_Form_Version_Id()
    {
        var formVersionId = new System.Guid("11111111-2222-3333-4444-555555555555");

        AiWorksheetSuggestionName.Build(formVersionId)
            .ShouldBe("ai-11111111222233334444555555555555-worksheet");
        AiScoresheetSuggestionName.Build(formVersionId)
            .ShouldBe("ai-11111111222233334444555555555555-scoresheet");
    }
}
