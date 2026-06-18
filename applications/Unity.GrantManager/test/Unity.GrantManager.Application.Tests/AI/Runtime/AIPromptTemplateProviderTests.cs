using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Unity.AI.Runtime;
using Unity.AI;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIPromptTemplateProviderTests
{
    [Fact]
    public async Task GetRequiredPromptAsync_Should_Return_Prompt_Definition_From_Database()
    {
        var prompt = new AIPrompt(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, PromptType.Skill);
        var version = new AIPromptVersion(
            Guid.NewGuid(),
            prompt.Id,
            1,
            "SYSTEM",
            "USER")
        {
            MetadataJson = "{\"sections\":{\"RULES\":\"- rule\"}}",
            IsPublished = true
        };

        var provider = CreateProvider(prompt, version);

        var snapshot = await provider.GetRequiredPromptAsync(AIPromptTypes.ApplicationAnalysis, "v1");

        snapshot.PromptVersion.ShouldBe("v1");
        snapshot.SystemPrompt.ShouldBe("SYSTEM");
        snapshot.UserPromptTemplate.ShouldBe("USER");
        snapshot.MetadataJson.ShouldBe("{\"sections\":{\"RULES\":\"- rule\"}}");
    }

    [Fact]
    public async Task GetRequiredPromptAsync_Should_Throw_When_Prompt_Is_Missing()
    {
        var provider = CreateProvider();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => provider.GetRequiredPromptAsync(AIPromptTypes.ApplicationAnalysis, "v1"));

        exception.Message.ShouldContain("ApplicationAnalysis");
    }

    private static AIPromptTemplateProvider CreateProvider(
        AIPrompt? prompt = null,
        AIPromptVersion? version = null)
    {
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var promptVersionRepository = Substitute.For<IRepository<AIPromptVersion, Guid>>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(null).Returns(Substitute.For<IDisposable>());

        promptRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>())
            .Returns(prompt);

        promptVersionRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIPromptVersion, bool>>>())
            .Returns(version);

        return new AIPromptTemplateProvider(promptRepository, promptVersionRepository, currentTenant);
    }
}
