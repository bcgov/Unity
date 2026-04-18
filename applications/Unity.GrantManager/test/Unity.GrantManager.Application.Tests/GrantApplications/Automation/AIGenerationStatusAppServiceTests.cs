using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationStatusAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GetLatestAsync_Should_Respect_Current_Tenant()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, "v1");

        var requests = new[]
        {
            new AIGenerationRequest(Guid.NewGuid(), otherTenantId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, applicationId, null, "v1", requestKey),
            new AIGenerationRequest(Guid.NewGuid(), tenantId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, applicationId, null, "v1", requestKey)
        };

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requests.AsQueryable()));

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var service = new AIGenerationStatusAppService(repository, currentTenant);

        var result = await service.GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType, "v1");

        result.ShouldNotBeNull();
        result!.TenantId.ShouldBe(tenantId);
        result.ApplicationId.ShouldBe(applicationId);
    }
}
