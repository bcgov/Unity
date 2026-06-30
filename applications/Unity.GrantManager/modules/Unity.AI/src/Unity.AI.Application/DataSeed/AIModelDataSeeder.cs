using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.DataSeed;

public class AIModelDataSeeder(
    IRepository<AIModel, Guid> modelRepository) : ITransientDependency
{
    private static readonly BuiltInModelDefinition[] BuiltInModels =
    [
        new("Gpt4oMini", true, 0.3d),
        new("Gpt5Mini", false, null),
        new("Gpt5Nano", false, null)
    ];

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null)
        {
            return;
        }

        foreach (var model in BuiltInModels)
        {
            await EnsureModelAsync(model);
        }
    }

    private async Task EnsureModelAsync(BuiltInModelDefinition definition)
    {
        var settings = new AIModelSettings
        {
            MaxOutputTokenCountSupported = definition.MaxOutputTokenCountSupported,
            Temperature = definition.Temperature
        };

        var existing = await modelRepository.FirstOrDefaultAsync(model => model.Name == definition.Name);
        if (existing != null)
        {
            existing.IsActive = true;
            existing.SettingsJson = JsonSerializer.Serialize(settings);
            await modelRepository.UpdateAsync(existing);
            return;
        }

        await modelRepository.InsertAsync(
            new AIModel(Guid.CreateVersion7(), definition.Name)
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(settings)
            },
            autoSave: true);
    }

    private sealed record BuiltInModelDefinition(
        string Name,
        bool MaxOutputTokenCountSupported,
        double? Temperature);
}
