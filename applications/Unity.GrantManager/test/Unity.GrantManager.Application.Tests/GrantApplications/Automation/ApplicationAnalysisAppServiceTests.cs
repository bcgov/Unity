using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
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

        var queue = Substitute.For<IApplicationAIGenerationQueue>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(Guid.NewGuid());

        var service = new ApplicationAnalysisAppService(queue, featureChecker, currentTenant);

        var result = await service.GenerateApplicationAnalysisAsync(Guid.NewGuid());

        result.ShouldNotBeNull();
        result.Completed.ShouldBeFalse();
        await queue.Received(1).QueueApplicationAnalysisAsync(Arg.Any<Guid>(), currentTenant.Id, Arg.Any<string?>());
    }
}
