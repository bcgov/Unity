using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.Prompts;

public class AIPromptAppServiceTests
{
    [Fact]
    public async Task DeleteAsync_Should_Reject_Global_Prompt_From_Tenant_Context()
    {
        var tenantId = Guid.NewGuid();
        var prompt = new AIPrompt(Guid.NewGuid(), "AttachmentSummary", 1, "system", "user");
        var repository = CreateRepository(prompt);
        var appService = CreateAppService(repository, tenantId);

        await Should.ThrowAsync<AbpAuthorizationException>(() => appService.DeleteAsync(prompt.Id));
        await repository.DidNotReceive().DeleteAsync(
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Prompt_From_Another_Tenant()
    {
        var tenantId = Guid.NewGuid();
        var prompt = new AIPrompt(Guid.NewGuid(), "AttachmentSummary", 1, "system", "user", Guid.NewGuid());
        var repository = CreateRepository(prompt);
        var appService = CreateAppService(repository, tenantId);

        await Should.ThrowAsync<AbpAuthorizationException>(() => appService.UpdateAsync(
            prompt.Id,
            new CreateUpdateAIPromptDto()));
    }

    private static IRepository<AIPrompt, Guid> CreateRepository(AIPrompt prompt)
    {
        var repository = Substitute.For<IRepository<AIPrompt, Guid>>();
        repository.GetAsync(prompt.Id).Returns(prompt);
        return repository;
    }

    private static AIPromptAppService CreateAppService(
        IRepository<AIPrompt, Guid> repository,
        Guid tenantId)
    {
        var dataFilter = Substitute.For<IDataFilter<IMultiTenant>>();
        dataFilter.Disable().Returns(Substitute.For<IDisposable>());
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        return new AIPromptAppService(repository, dataFilter, currentTenant);
    }
}
