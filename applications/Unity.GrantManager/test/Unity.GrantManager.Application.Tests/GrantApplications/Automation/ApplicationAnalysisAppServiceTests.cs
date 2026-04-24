using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Features;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class ApplicationAnalysisAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GenerateApplicationAnalysisAsync_Should_Return_Completed_Result()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(true);

        var analysisService = Substitute.For<IApplicationAnalysisService>();
        analysisService.RegenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>()).Returns("analysis");

        var service = new ApplicationAnalysisAppService(analysisService, featureChecker);

        var result = await service.GenerateApplicationAnalysisAsync(Guid.NewGuid());

        result.ShouldNotBeNull();
        result.Completed.ShouldBeTrue();
    }
}
