using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Unity.AI.Runtime;
using Unity.AI;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIPromptTemplateProviderTests
{
    [Fact]
    public async Task GetRequiredPromptAsync_Should_Return_Prompt_Definition_From_Database()
    {
        var prompt = new AIPrompt(
            Guid.NewGuid(),
            AIPromptTypes.ApplicationAnalysis,
            1,
            "SYSTEM",
            "USER")
        {
            MetadataJson = "{\"operationName\":\"ApplicationAnalysis\",\"promptVersion\":\"v1\",\"inputContractName\":\"ApplicationAnalysisOperationInputDto\",\"outputContractName\":\"ApplicationAnalysisResponse\"}",
            IsActive = true
        };

        var provider = CreateProvider(prompt);

        var snapshot = await provider.GetRequiredPromptAsync(AIPromptTypes.ApplicationAnalysis, "v1");

        snapshot.PromptVersion.ShouldBe("v1");
        snapshot.SystemPrompt.ShouldBe("SYSTEM");
        snapshot.UserPrompt.ShouldBe("USER");
        snapshot.MetadataJson.ShouldContain("ApplicationAnalysis");
        snapshot.Manifest.ShouldNotBeNull();
        snapshot.Manifest!.OperationName.ShouldBe("ApplicationAnalysis");
        snapshot.Manifest.PromptVersion.ShouldBe("v1");
        snapshot.Manifest.InputContractName.ShouldBe("ApplicationAnalysisOperationInputDto");
        snapshot.Manifest.OutputContractName.ShouldBe("ApplicationAnalysisResponse");
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
        AIPrompt? prompt = null)
    {
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var multiTenantDataFilter = Substitute.For<IDataFilter<IMultiTenant>>();
        multiTenantDataFilter.Disable().Returns(Substitute.For<IDisposable>());

        promptRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>();
                return Task.FromResult(prompt != null && predicate.Compile()(prompt) ? prompt : null);
            });

        return new AIPromptTemplateProvider(promptRepository, multiTenantDataFilter);
    }
}
