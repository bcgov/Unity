using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
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
        var operationId = Guid.NewGuid();

        var operations = new[]
        {
            new AIOperation(operationId, "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid())
            {
                IsActive = true
            }
        };

        var requests = new[]
        {
            new AIGenerationRequest(Guid.NewGuid(), otherTenantId, operationId, applicationId),
            new AIGenerationRequest(Guid.NewGuid(), tenantId, operationId, applicationId)
        };

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requests.AsQueryable()));
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));
        var asyncQueryableExecuter = new AsyncQueryableExecuter(Array.Empty<IAsyncQueryableProvider>());

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var service = new AIGenerationStatusAppService(repository, operationRepository, currentTenant, asyncQueryableExecuter);

        var result = await service.GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);

        result.ShouldNotBeNull();
        result.ApplicationId.ShouldBe(applicationId);
    }

    [Fact]
    public async Task GetLatestAsync_Should_Return_History_For_Inactive_Operation()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var operations = new[]
        {
            new AIOperation(operationId, "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid())
            {
                IsActive = false
            }
        };

        var requests = new[]
        {
            new AIGenerationRequest(Guid.NewGuid(), tenantId, operationId, applicationId)
        };

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requests.AsQueryable()));
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));
        var asyncQueryableExecuter = new AsyncQueryableExecuter(Array.Empty<IAsyncQueryableProvider>());

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var service = new AIGenerationStatusAppService(repository, operationRepository, currentTenant, asyncQueryableExecuter);

        var result = await service.GetLatestAsync(applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);

        result.ShouldNotBeNull();
        result!.OperationId.ShouldBe(operationId);
        result.ApplicationId.ShouldBe(applicationId);
    }

    [Fact]
    public async Task GetLatestAsync_Should_Reject_Legacy_Operation_Name()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var operations = new[]
        {
            new AIOperation(operationId, "ApplicationAnalysis", Guid.NewGuid(), Guid.NewGuid())
        };

        var requests = new[]
        {
            new AIGenerationRequest(Guid.NewGuid(), tenantId, operationId, applicationId)
        };

        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();
        repository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requests.AsQueryable()));
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository.GetQueryableAsync().Returns(Task.FromResult<IQueryable<AIOperation>>(operations.AsQueryable()));
        var asyncQueryableExecuter = new AsyncQueryableExecuter(Array.Empty<IAsyncQueryableProvider>());

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var service = new AIGenerationStatusAppService(repository, operationRepository, currentTenant, asyncQueryableExecuter);

        var result = await service.GetLatestAsync(applicationId, "legacy-operation-name");

        result.ShouldBeNull();
    }
}
