using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.DataSeed;
using Unity.AI.Domain;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.AI.DataSeed;

public class AIModelDataSeederTests
{
    [Fact]
    public async Task Should_Seed_All_Configured_Models()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var insertedModels = new List<AIModel>();
        modelRepository
            .GetListAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<AIModel, bool>>>(),
                cancellationToken: Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new List<AIModel>()));
        modelRepository
            .InsertAsync(Arg.Any<AIModel>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo =>
            {
                var model = callInfo.Arg<AIModel>();
                ArgumentNullException.ThrowIfNull(model);
                insertedModels.Add(model);
                return Task.FromResult(model);
            });

        var seeder = new AIModelDataSeeder(modelRepository);

        await seeder.SeedAsync(new DataSeedContext());

        insertedModels.Count.ShouldBe(3);
        insertedModels.ShouldContain(model => model.Name == "gpt-4o-mini" && model.Provider == "OpenAI" && model.IsActive);
        insertedModels.ShouldContain(model => model.Name == "gpt-5-mini" && model.Provider == "OpenAI" && model.IsActive);
        insertedModels.ShouldContain(model => model.Name == "gpt-5-nano" && model.Provider == "OpenAI" && model.IsActive);

        var gpt4oMini = insertedModels.Single(model => model.Name == "gpt-4o-mini");
        var gpt5Mini = insertedModels.Single(model => model.Name == "gpt-5-mini");
        var gpt5Nano = insertedModels.Single(model => model.Name == "gpt-5-nano");

        DeserializeSettings(gpt4oMini.SettingsJson).MaxOutputTokenCountSupported.ShouldBeTrue();
        DeserializeSettings(gpt4oMini.SettingsJson).Temperature.ShouldBe(0.3);
        DeserializeSettings(gpt5Mini.SettingsJson).MaxOutputTokenCountSupported.ShouldBeFalse();
        DeserializeSettings(gpt5Mini.SettingsJson).Temperature.ShouldBeNull();
        DeserializeSettings(gpt5Nano.SettingsJson).MaxOutputTokenCountSupported.ShouldBeFalse();
        DeserializeSettings(gpt5Nano.SettingsJson).Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Update_Existing_Model_When_Provider_Is_Stale()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var existingModel = new AIModel(Guid.NewGuid(), "gpt-5-mini", "LegacyProvider")
        {
            IsActive = false,
            SettingsJson = "{}"
        };
        modelRepository
            .GetListAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<AIModel, bool>>>(),
                cancellationToken: Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo =>
            {
                var predicateExpression = callInfo
                    .Arg<System.Linq.Expressions.Expression<Func<AIModel, bool>>>();
                ArgumentNullException.ThrowIfNull(predicateExpression);
                var predicate = predicateExpression.Compile();
                return Task.FromResult(new[] { existingModel }.Where(predicate).ToList());
            });

        var seeder = new AIModelDataSeeder(modelRepository);

        await seeder.SeedAsync(new DataSeedContext());

        existingModel.Provider.ShouldBe("OpenAI");
        existingModel.IsActive.ShouldBeTrue();
        DeserializeSettings(existingModel.SettingsJson).MaxOutputTokenCountSupported.ShouldBeFalse();
        await modelRepository.Received(1).UpdateAsync(existingModel, autoSave: true);
        await modelRepository.DidNotReceive().InsertAsync(
            Arg.Is<AIModel>(model => model != null && model.Name == existingModel.Name),
            Arg.Any<bool>(),
            Arg.Any<System.Threading.CancellationToken>());
    }

    private static AIModelSettings DeserializeSettings(string settingsJson)
    {
        var settings = JsonSerializer.Deserialize<AIModelSettings>(settingsJson);
        settings.ShouldNotBeNull();
        return settings;
    }
}
