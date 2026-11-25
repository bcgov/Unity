using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Prompts;

public class AIPromptDeleteTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private IAIPromptAppService PromptService => GetRequiredService<IAIPromptAppService>();
    private IRepository<AIPrompt, Guid> PromptRepository => GetRequiredService<IRepository<AIPrompt, Guid>>();

    [Fact]
    public async Task Should_Mark_Highest_Active_Version_As_In_Use()
    {
        var name = NewName();
        var v1 = await InsertAsync(name, 1, isActive: true);
        var v2 = await InsertAsync(name, 2, isActive: true);
        var v3 = await InsertAsync(name, 3, isActive: false);

        var list = await WithUnitOfWorkAsync(() =>
            PromptService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 }));

        var rows = list.Items.Where(item => item.Name == name).ToDictionary(item => item.Id);
        rows[v1.Id].IsInUse.ShouldBeFalse();
        rows[v2.Id].IsInUse.ShouldBeTrue();
        rows[v3.Id].IsInUse.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Deleting_The_Version_In_Use()
    {
        var name = NewName();
        await InsertAsync(name, 1, isActive: true);
        var inUse = await InsertAsync(name, 2, isActive: true);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(() => PromptService.DeleteAsync(inUse.Id)));

        exception.Code.ShouldBe(AIErrorCodes.PromptVersionInUse);
        (await WithUnitOfWorkAsync(() => PromptRepository.FindAsync(inUse.Id))).ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Delete_Older_And_Inactive_Versions()
    {
        var name = NewName();
        var older = await InsertAsync(name, 1, isActive: true);
        await InsertAsync(name, 2, isActive: true);
        var inactiveNewer = await InsertAsync(name, 3, isActive: false);

        await WithUnitOfWorkAsync(() => PromptService.DeleteAsync(older.Id));
        await WithUnitOfWorkAsync(() => PromptService.DeleteAsync(inactiveNewer.Id));

        (await WithUnitOfWorkAsync(() => PromptRepository.FindAsync(older.Id))).ShouldBeNull();
        (await WithUnitOfWorkAsync(() => PromptRepository.FindAsync(inactiveNewer.Id))).ShouldBeNull();
    }

    private static string NewName() => "DeleteTest" + Guid.NewGuid().ToString("N")[..8];

    private Task<AIPrompt> InsertAsync(string name, int version, bool isActive)
    {
        return WithUnitOfWorkAsync(() => PromptRepository.InsertAsync(
            new AIPrompt(Guid.NewGuid(), name, version, "system", "user") { IsActive = isActive },
            autoSave: true));
    }
}
