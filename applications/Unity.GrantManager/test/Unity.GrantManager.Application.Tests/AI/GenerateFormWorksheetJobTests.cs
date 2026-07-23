using System;
using Shouldly;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Xunit;

namespace Unity.GrantManager.AI;

public class GenerateFormWorksheetJobTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"Title":"Draft","Sections":[]}""")]
    public void ParseWorksheetDefinition_Should_Reject_Incomplete_Ai_Response(string worksheetJson)
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            GenerateFormWorksheetJob.ParseWorksheetDefinition(worksheetJson));

        exception.Message.ShouldContain("unusable worksheet definition");
    }
}
