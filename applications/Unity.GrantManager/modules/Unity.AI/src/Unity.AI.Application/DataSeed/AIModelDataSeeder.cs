using System;
using System.Collections.Generic;
using System.Linq;
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
        new("gpt-5-mini", "OpenAI", false, null),
        new("gpt-5-nano", "OpenAI", false, null)
    ];

    // Azure OpenAI no longer allows new deployments of these, so existing rows are deactivated, not deleted
    private static readonly string[] RetiredModelNames =
    [
        "gpt-4o-mini"
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

        foreach (var modelName in RetiredModelNames)
        {
            await DeactivateModelAsync(modelName);
        }
    }

    private async Task EnsureModelAsync(BuiltInModelDefinition definition)
    {
        var settings = new AIModelSettings
        {
            MaxOutputTokenCountSupported = definition.MaxOutputTokenCountSupported,
            Temperature = definition.Temperature
        };

        var existing = (await modelRepository.GetListAsync(model =>
            model.Name == definition.Name)).SingleOrDefault();
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

    private async Task DeactivateModelAsync(string modelName)
    {
        var existing = (await modelRepository.GetListAsync(model =>
            model.Name == modelName)).SingleOrDefault();
        if (existing == null || !existing.IsActive)
        {
            return;
        }

        existing.IsActive = false;
        await modelRepository.UpdateAsync(existing, autoSave: true);
    }

    private sealed record BuiltInModelDefinition(
        string Name,
        string Provider,
        bool MaxOutputTokenCountSupported,
        double? Temperature);
}
