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
    public async Task Should_Seed_All_Configured_Profiles()
    {
        var modelRepository = Substitute.For<IRepository<AIModel, Guid>>();
        var insertedModels = new List<AIModel>();
        modelRepository
            .FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AIModel, bool>>>())
            .Returns((AIModel?)null);
        modelRepository
            .InsertAsync(Arg.Any<AIModel>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(callInfo =>
            {
                var model = callInfo.Arg<AIModel>();
                insertedModels.Add(model);
                return Task.FromResult(model);
            });

        var seeder = new AIModelDataSeeder(modelRepository);

        await seeder.SeedAsync(new DataSeedContext());

        insertedModels.Count.ShouldBe(3);
        insertedModels.ShouldContain(model => model.Name == "Gpt4oMini" && model.IsActive);
        insertedModels.ShouldContain(model => model.Name == "Gpt5Mini" && model.IsActive);
        insertedModels.ShouldContain(model => model.Name == "Gpt5Nano" && model.IsActive);

        var gpt4oMini = insertedModels.Single(model => model.Name == "Gpt4oMini");
        var gpt5Mini = insertedModels.Single(model => model.Name == "Gpt5Mini");
        var gpt5Nano = insertedModels.Single(model => model.Name == "Gpt5Nano");

        DeserializeSettings(gpt4oMini.SettingsJson).MaxOutputTokenCountSupported.ShouldBeTrue();
        DeserializeSettings(gpt4oMini.SettingsJson).Temperature.ShouldBe(0.3);
        DeserializeSettings(gpt5Mini.SettingsJson).MaxOutputTokenCountSupported.ShouldBeFalse();
        DeserializeSettings(gpt5Mini.SettingsJson).Temperature.ShouldBeNull();
        DeserializeSettings(gpt5Nano.SettingsJson).MaxOutputTokenCountSupported.ShouldBeFalse();
        DeserializeSettings(gpt5Nano.SettingsJson).Temperature.ShouldBeNull();
    }

    private static AIModelSettings DeserializeSettings(string settingsJson)
    {
        var settings = JsonSerializer.Deserialize<AIModelSettings>(settingsJson);
        settings.ShouldNotBeNull();
        return settings;
    }
}
