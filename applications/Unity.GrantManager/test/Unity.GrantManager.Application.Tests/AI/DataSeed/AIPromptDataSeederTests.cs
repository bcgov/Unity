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
            .ReturnsForAnyArgs(callInfo =>
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

        var worksheetPrompt = insertedPrompts.Single(prompt => prompt.Name == AIPromptTypes.FormWorksheet);
        worksheetPrompt.UserPrompt.ShouldContain("all applicable field suggestions");
        worksheetPrompt.UserPrompt.ShouldContain("empty fields array");
        var scoresheetPrompt = insertedPrompts.Single(prompt => prompt.Name == AIPromptTypes.FormScoresheet);
        scoresheetPrompt.UserPrompt.ShouldContain("min");
        scoresheetPrompt.UserPrompt.ShouldContain("max");
        var mappingPrompt = insertedPrompts.Single(prompt => prompt.Name == AIPromptTypes.FormMapping);
        mappingPrompt.VersionNumber.ShouldBe(2);
        mappingPrompt.UserPrompt.ShouldContain("Down-weight CHEFS fields");
        mappingPrompt.UserPrompt.ShouldContain("Up-weight CHEFS fields");
        mappingPrompt.UserPrompt.ShouldContain("prefer the hidden-field signal");
    }


    [Fact]
    public async Task Should_Not_Recreate_Older_Version_Deleted_When_Newer_Exists()
    {
        var newest = new AIPrompt(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, 2, "system", "user");
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var insertedPrompts = new List<AIPrompt>();
        promptRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIPrompt, bool>>>().Compile();
                return Task.FromResult<AIPrompt>(predicate(newest) ? newest : null!);
            });
        promptRepository
            .InsertAsync(Arg.Any<AIPrompt>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var prompt = callInfo.Arg<AIPrompt>();
                insertedPrompts.Add(prompt);
                return Task.FromResult(prompt);
            });

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(null).Returns(Substitute.For<IDisposable>());
        var seeder = new AIPromptDataSeeder(promptRepository, currentTenant);
        await seeder.SeedAsync(new DataSeedContext());

        // v0 and v1 were deleted in AI Prompts; v2 is there, so neither comes back.
        insertedPrompts.ShouldNotContain(prompt => prompt.Name == AIPromptTypes.ApplicationAnalysis);
        AssertVersions(insertedPrompts, AIPromptTypes.AttachmentSummary, 0, 1, 2);
    }

    [Fact]
    public async Task Should_Not_Change_Existing_Prompt_Version()
    {
        var existingPrompt = new AIPrompt(Guid.NewGuid(), AIPromptTypes.FormWorksheet, 2, "old system prompt", "old user prompt")
        {
            MetadataJson = "old metadata",
            IsActive = false
        };
        var promptName = existingPrompt.Name;
        var promptVersion = existingPrompt.VersionNumber;
        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        promptRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIPrompt, bool>>>().Compile();
                var probe = new AIPrompt(Guid.NewGuid(), promptName, promptVersion, string.Empty, string.Empty);
                return Task.FromResult<AIPrompt>(predicate(probe) ? existingPrompt : null!);
            });
        promptRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AIPrompt, bool>>>(), Arg.Any<System.Threading.CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var predicate = callInfo.Arg<Expression<Func<AIPrompt, bool>>>().Compile();
                var probe = new AIPrompt(Guid.NewGuid(), promptName, promptVersion, string.Empty, string.Empty);
                return Task.FromResult<AIPrompt>(predicate(probe) ? existingPrompt : null!);
            });
        promptRepository
            .UpdateAsync(Arg.Any<AIPrompt>(), true, Arg.Any<System.Threading.CancellationToken>())
            .ReturnsForAnyArgs(callInfo => Task.FromResult(callInfo.Arg<AIPrompt>()));

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(null).Returns(Substitute.For<IDisposable>());
        var seeder = new AIPromptDataSeeder(promptRepository, currentTenant);
        await seeder.SeedAsync(new DataSeedContext());

        // Insert-only: an edit or deactivation made in AI Prompts survives the next deploy.
        existingPrompt.SystemPrompt.ShouldBe("old system prompt");
        existingPrompt.UserPrompt.ShouldBe("old user prompt");
        existingPrompt.MetadataJson.ShouldBe("old metadata");
        existingPrompt.IsActive.ShouldBeFalse();
        await promptRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default, default);
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
