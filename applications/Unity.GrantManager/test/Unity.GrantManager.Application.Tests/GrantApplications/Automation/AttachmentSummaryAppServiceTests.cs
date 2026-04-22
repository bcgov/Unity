using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.AI.Operations;
using Volo.Abp.Features;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AttachmentSummaryAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GenerateAttachmentSummaryAsync_Should_Return_Completed_Result()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);

        var summaryService = Substitute.For<IAttachmentSummaryService>();
        summaryService.GenerateAndSaveAsync(Arg.Any<Guid>(), Arg.Any<string?>()).Returns("summary");

        var service = new AttachmentSummaryAppService(summaryService, featureChecker);

        var result = await service.GenerateAttachmentSummaryAsync(Guid.NewGuid());

        result.ShouldNotBeNull();
        result.Completed.ShouldBeTrue();
    }

    [Fact]
    public async Task GenerateAttachmentSummariesAsync_Should_Return_Completed_Results()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);

        var summaryService = Substitute.For<IAttachmentSummaryService>();
        summaryService.GenerateAndSaveAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new List<string> { "summary" }));

        var service = new AttachmentSummaryAppService(summaryService, featureChecker);

        var result = await service.GenerateAttachmentSummariesAsync(new List<Guid> { Guid.NewGuid() });

        result.ShouldHaveSingleItem();
        result[0].Completed.ShouldBeTrue();
    }
}
