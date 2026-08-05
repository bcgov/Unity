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
        new("gpt-4o-mini", "OpenAI", true, 0.3d),
        new("gpt-5-mini", "OpenAI", false, null),
        new("gpt-5-nano", "OpenAI", false, null)
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

        var existing = await modelRepository.FirstOrDefaultAsync(model =>
            model.Name == definition.Name && model.Provider == definition.Provider);
        if (existing != null)
        {
            existing.Provider = definition.Provider;
            existing.IsActive = true;
            existing.SettingsJson = JsonSerializer.Serialize(settings);
            await modelRepository.UpdateAsync(existing, autoSave: true);
            return;
        }

        await modelRepository.InsertAsync(
            new AIModel(Guid.CreateVersion7(), definition.Name, definition.Provider)
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(settings)
            },
            autoSave: true);
    }

    private sealed record BuiltInModelDefinition(
        string Name,
        string Provider,
        bool MaxOutputTokenCountSupported,
        double? Temperature);
}
