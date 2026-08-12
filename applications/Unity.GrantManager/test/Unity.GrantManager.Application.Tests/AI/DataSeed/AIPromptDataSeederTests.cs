using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.AI.DataSeed;
using Unity.AI.Domain;
using Unity.AI.Runtime.Prompts;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.DataSeed;

public class AIPromptDataSeederTests
{
    [Fact]
    public async Task Should_Seed_The_Complete_BuiltIn_Prompt_Matrix()
    {
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var insertedPrompts = new List<AIPrompt>();
        promptRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>())
            .Returns((AIPrompt?)null);
        promptRepository
            .InsertAsync(Arg.Any<AIPrompt>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo =>
            {
                var prompt = callInfo.Arg<AIPrompt>();
                ArgumentNullException.ThrowIfNull(prompt);
                insertedPrompts.Add(prompt);
                return Task.FromResult(prompt);
            });

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(null).Returns(Substitute.For<IDisposable>());
        var seeder = new AIPromptDataSeeder(promptRepository, currentTenant);

        await seeder.SeedAsync(new DataSeedContext());

        insertedPrompts.Count.ShouldBe(12);
        AssertVersions(insertedPrompts, AIPromptTypes.ApplicationAnalysis, 0, 1, 2);
        AssertVersions(insertedPrompts, AIPromptTypes.AttachmentSummary, 0, 1, 2);
        AssertVersions(insertedPrompts, AIPromptTypes.ApplicationScoring, 0, 1, 2);
        AssertVersions(insertedPrompts, AIPromptTypes.FormMapping, 2);
        AssertVersions(insertedPrompts, AIPromptTypes.FormWorksheet, 2);
        AssertVersions(insertedPrompts, AIPromptTypes.FormScoresheet, 2);
        insertedPrompts.All(prompt =>
            prompt.TenantId is null &&
            prompt.IsActive &&
            !string.IsNullOrWhiteSpace(prompt.SystemPrompt) &&
            !string.IsNullOrWhiteSpace(prompt.UserPrompt)).ShouldBeTrue();
    }

    private static void AssertVersions(
        IEnumerable<AIPrompt> prompts,
        string promptName,
        params int[] expectedVersions)
    {
        prompts
            .Where(prompt => prompt.Name == promptName)
            .Select(prompt => prompt.VersionNumber)
            .OrderBy(version => version)
            .ShouldBe(expectedVersions);
    }
}
