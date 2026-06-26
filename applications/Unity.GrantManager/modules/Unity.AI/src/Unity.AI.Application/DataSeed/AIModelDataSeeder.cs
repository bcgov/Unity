using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.DataSeed;

public class AIModelDataSeeder(
    IRepository<AIModel, Guid> modelRepository,
    IConfiguration configuration) : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null)
        {
            return;
        }

        var providerName = Optional("Azure:Operations:Defaults:Provider");
        var defaultProfileName = Optional("Azure:Operations:Defaults:Profile");
        if (providerName == null || defaultProfileName == null)
        {
            return;
        }

        var profilesSection = configuration.GetSection($"Azure:{providerName}:Profiles");
        var profileNames = profilesSection
            .GetChildren()
            .Select(section => section.Key)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!profileNames.Contains(defaultProfileName, StringComparer.OrdinalIgnoreCase))
        {
            profileNames.Add(defaultProfileName);
        }

        foreach (var profileName in profileNames)
        {
            var deploymentName = Optional($"Azure:{providerName}:Profiles:{profileName}:DeploymentName");
            if (deploymentName == null)
            {
                continue;
            }

            await EnsureModelAsync(
                profileName,
                new AIModelSettings
                {
                    MaxOutputTokenCountSupported = OptionalBool($"Azure:{providerName}:Profiles:{profileName}:MaxOutputTokenCountSupported") ?? true,
                    Temperature = OptionalDouble($"Azure:{providerName}:Profiles:{profileName}:Temperature")
                });
        }

    }

    private async Task EnsureModelAsync(string modelName, AIModelSettings settings)
    {
        var existing = await modelRepository.FirstOrDefaultAsync(model => model.Name == modelName);
        if (existing != null)
        {
            existing.IsActive = true;
            existing.SettingsJson = JsonSerializer.Serialize(settings);
            await modelRepository.UpdateAsync(existing);
            return;
        }

        await modelRepository.InsertAsync(
            new AIModel(Guid.CreateVersion7(), modelName)
            {
                IsActive = true,
                SettingsJson = JsonSerializer.Serialize(settings)
            });
    }

    private string? Optional(string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private bool? OptionalBool(string key)
    {
        var value = Optional(key);
        return value != null && bool.TryParse(value, out var parsedValue)
            ? parsedValue
            : null;
    }

    private double? OptionalDouble(string key)
    {
        var value = Optional(key);
        return value != null && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }
}
