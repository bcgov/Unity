using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Unity.AI;
using Unity.AI.DataSeed;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.AI.DataSeed;

public class AIOperationDataSeederTests
{
    [Fact]
    public async Task Should_Seed_Configured_Default_Profile_Model_When_Missing()
    {
        var operationRepository = Substitute.For<IRepository<AIOperation, Guid>>();
        operationRepository
            .GetQueryableAsync()
            .Returns(Task.FromResult(new List<AIOperation>().AsQueryable()));

        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var insertedModels = new List<AIModel>();
        modelRepository
            .GetQueryableAsync()
            .Returns(Task.FromResult(new List<AIModel>().AsQueryable()));
        modelRepository
            .InsertAsync(Arg.Any<AIModel>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var model = callInfo.Arg<AIModel>();
                insertedModels.Add(model);
                return Task.FromResult(model);
            });

        var insertedOperations = new List<AIOperation>();
        operationRepository
            .InsertAsync(Arg.Any<AIOperation>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<AIOperation>();
                insertedOperations.Add(operation);
                return Task.FromResult(operation);
            });

        var promptRepository = Substitute.For<IRepository<AIPrompt, Guid>>();
        var prompts = new List<AIPrompt>
        {
            new(Guid.NewGuid(), AIPromptTypes.ApplicationAnalysis, 1, "system", "user"),
            new(Guid.NewGuid(), AIPromptTypes.AttachmentSummary, 1, "system", "user"),
            new(Guid.NewGuid(), AIPromptTypes.ApplicationScoring, 1, "system", "user")
        };
        promptRepository
            .GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<System.Linq.Expressions.Expression<Func<AIPrompt, bool>>>().Compile();
                return Task.FromResult(prompts.Where(predicate).ToList());
            });

        var currentTenant = Substitute.For<ICurrentTenant>();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
            ["Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName"] = "gpt-5-mini",
            ["Azure:OpenAI:Endpoint"] = "https://example.test",
            ["Azure:Operations:ApplicationAnalysis:MaxCompletionTokens"] = "4000",
            ["Azure:Operations:AttachmentSummary:MaxCompletionTokens"] = "2000",
            ["Azure:Operations:ApplicationScoring:MaxCompletionTokens"] = "8000"
        });

        var seeder = new AIOperationDataSeeder(
            operationRepository,
            modelRepository,
            promptRepository,
            configuration,
            currentTenant,
            Substitute.For<ILogger<AIOperationDataSeeder>>());

        await seeder.SeedAsync(new DataSeedContext());

        insertedModels.Count.ShouldBe(1);
        insertedModels[0].Name.ShouldBe("Gpt5Mini");
        DeserializeSettings(insertedModels[0].SettingsJson).MaxOutputTokenCountSupported.ShouldBeTrue();
        DeserializeSettings(insertedModels[0].SettingsJson).Temperature.ShouldBeNull();

    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static AIModelSettings DeserializeSettings(string settingsJson)
    {
        var settings = JsonSerializer.Deserialize<AIModelSettings>(settingsJson);
        settings.ShouldNotBeNull();
        return settings;
    }
}
