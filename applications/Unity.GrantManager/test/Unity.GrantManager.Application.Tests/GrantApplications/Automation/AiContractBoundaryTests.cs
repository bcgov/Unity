using Shouldly;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AiContractBoundaryTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public void IApplicationScoringAppService_Should_Be_Resolvable_From_Remote_Contract_Boundary()
    {
        var service = GetRequiredService<IApplicationScoringAppService>();

        service.ShouldNotBeNull();
        service.GetType().Name.ShouldContain("Proxy");
    }

    [Fact]
    public void IApplicationAnalysisAppService_Should_Be_Resolvable_From_Remote_Contract_Boundary()
    {
        var service = GetRequiredService<IApplicationAnalysisAppService>();

        service.ShouldNotBeNull();
        service.GetType().Name.ShouldContain("Proxy");
    }

    [Fact]
    public void IApplicationContentAppService_Should_Be_Resolvable_From_Remote_Contract_Boundary()
    {
        var service = GetRequiredService<IApplicationContentAppService>();

        service.ShouldNotBeNull();
        service.GetType().Name.ShouldContain("Proxy");
    }

    [Fact]
    public void IAttachmentSummaryAppService_Should_Be_Resolvable_From_Remote_Contract_Boundary()
    {
        var service = GetRequiredService<IAttachmentSummaryAppService>();

        service.ShouldNotBeNull();
        service.GetType().Name.ShouldContain("Proxy");
    }
}
