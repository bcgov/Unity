using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
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
            .InsertAsync(Arg.Any<AIModel>())
            .Returns(callInfo =>
            {
                var model = callInfo.Arg<AIModel>();
                insertedModels.Add(model);
                return Task.FromResult(model);
            });

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
            ["Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName"] = "gpt-5-mini",
            ["Azure:OpenAI:Profiles:Gpt5Mini:Temperature"] = "0.15",
            ["Azure:OpenAI:Profiles:Gpt5Nano:DeploymentName"] = "gpt-5-nano",
            ["Azure:OpenAI:Endpoint"] = "https://example.test"
        });

        var seeder = new AIModelDataSeeder(modelRepository, configuration);

        await seeder.SeedAsync(new DataSeedContext());

        insertedModels.Count.ShouldBe(2);
        insertedModels.ShouldContain(model => model.Name == "Gpt5Mini" && model.IsActive);
        insertedModels.ShouldContain(model => model.Name == "Gpt5Nano" && model.IsActive);

        var gpt5MiniModel = insertedModels.Find(model => model.Name == "Gpt5Mini");
        gpt5MiniModel.ShouldNotBeNull();
        DeserializeSettings(gpt5MiniModel.SettingsJson).MaxOutputTokenCountSupported.ShouldBeTrue();
        DeserializeSettings(gpt5MiniModel.SettingsJson).Temperature.ShouldBe(0.15);
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
