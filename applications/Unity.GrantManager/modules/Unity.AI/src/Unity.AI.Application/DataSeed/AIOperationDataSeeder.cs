using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Prompts;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.DataSeed;

public class AIOperationDataSeeder(
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIPrompt, Guid> promptRepository,
    IConfiguration configuration,
    ICurrentTenant currentTenant,
    ILogger<AIOperationDataSeeder> logger) : IDataSeedContributor, ITransientDependency
{
    private const string DefaultOperationName = "Default";
    private const string DefaultOperationPromptType = AIPromptTypes.ApplicationAnalysis;

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null)
        {
            return;
        }

        using (currentTenant.Change(null))
        {
            var model = await EnsureConfiguredDefaultModelAsync();
            if (model == null)
            {
                logger.LogWarning("AI operation seeding skipped: no model is configured. Check Azure:Operations:Defaults:Provider and Azure:Operations:Defaults:Profile settings.");
                return;
            }

            await EnsureOperationAsync(DefaultOperationName, DefaultOperationPromptType, model, isDefaultOperation: true);
            await EnsureOperationAsync(AIPromptTypes.ApplicationAnalysis, AIPromptTypes.ApplicationAnalysis, model);
            await EnsureOperationAsync(AIPromptTypes.AttachmentSummary, AIPromptTypes.AttachmentSummary, model);
            await EnsureOperationAsync(AIPromptTypes.ApplicationScoring, AIPromptTypes.ApplicationScoring, model);
        }
    }

    private async Task EnsureOperationAsync(
        string operationName,
        string promptName,
        AIModel model,
        bool isDefaultOperation = false)
    {
        var prompt = await ResolveLatestActivePromptAsync(promptName);
        if (prompt == null)
        {
            if (isDefaultOperation)
            {
                logger.LogWarning("AI default operation seeding skipped: no active prompt found for '{PromptName}'.", promptName);
            }
            else
            {
                logger.LogWarning("AI operation seeding skipped: no active prompt found for operation '{OperationName}' and prompt '{PromptName}'.", operationName, promptName);
            }
            return;
        }

        var completionTokens = ResolveCompletionTokens(promptName);
        if (completionTokens == null)
        {
            if (isDefaultOperation)
            {
                logger.LogWarning("AI default operation seeding skipped: MaxCompletionTokens not configured for '{PromptName}'.", promptName);
            }
            else
            {
                logger.LogWarning("AI operation seeding skipped: MaxCompletionTokens not configured for operation '{OperationName}' (prompt '{PromptName}').", operationName, promptName);
            }
            return;
        }

        var existing = await operationRepository.FirstOrDefaultAsync(op => op.Name == operationName);
        if (existing != null)
        {
            existing.AIModelId = model.Id;
            existing.AIPromptId = prompt.Id;
            existing.ExecutionMode = AIExecutionMode.Sequential;
            existing.CompletionTokens = completionTokens.Value;
            existing.IsActive = true;
            await operationRepository.UpdateAsync(existing);
            return;
        }

        await operationRepository.InsertAsync(
            new AIOperation(Guid.CreateVersion7(), operationName, model.Id, prompt.Id)
            {
                ExecutionMode = AIExecutionMode.Sequential,
                CompletionTokens = completionTokens.Value,
                IsActive = true
            });
    }

    private async Task<AIModel?> EnsureConfiguredDefaultModelAsync()
    {
        var providerName = Optional("Azure:Operations:Defaults:Provider");
        var profileName = Optional("Azure:Operations:Defaults:Profile");
        if (providerName == null || profileName == null)
        {
            return null;
        }

        var deploymentName = Optional($"Azure:{providerName}:Profiles:{profileName}:DeploymentName");
        if (deploymentName == null)
        {
            return null;
        }

        var existing = await modelRepository.FirstOrDefaultAsync(model => model.Name == profileName && model.IsActive);
        if (existing != null)
        {
            existing.SettingsJson = System.Text.Json.JsonSerializer.Serialize(new AIModelSettings
            {
                MaxOutputTokenCountSupported = OptionalBool($"Azure:{providerName}:Profiles:{profileName}:MaxOutputTokenCountSupported") ?? true,
                Temperature = OptionalDouble($"Azure:{providerName}:Profiles:{profileName}:Temperature")
            });
            await modelRepository.UpdateAsync(existing);
            return existing;
        }

        var settings = new AIModelSettings
        {
            MaxOutputTokenCountSupported = OptionalBool($"Azure:{providerName}:Profiles:{profileName}:MaxOutputTokenCountSupported") ?? true,
            Temperature = OptionalDouble($"Azure:{providerName}:Profiles:{profileName}:Temperature")
        };

        var existingInactive = await modelRepository.FirstOrDefaultAsync(model => model.Name == profileName);
        if (existingInactive != null)
        {
            existingInactive.IsActive = true;
            existingInactive.SettingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
            return await modelRepository.UpdateAsync(existingInactive);
        }

        var model = await modelRepository.InsertAsync(
            new AIModel(Guid.CreateVersion7(), profileName)
            {
                IsActive = true,
                SettingsJson = System.Text.Json.JsonSerializer.Serialize(settings)
            });

        return model;
    }

    private async Task<AIPrompt?> ResolveLatestActivePromptAsync(string promptName)
    {
        var prompts = await promptRepository.GetListAsync(item => item.Name == promptName && item.IsActive);
        return prompts
            .OrderByDescending(prompt => prompt.VersionNumber)
            .FirstOrDefault();
    }

    private string? Optional(string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private int? OptionalPositiveInt(string key)
    {
        var value = configuration.GetValue<int?>(key);
        return value is > 0 ? value : null;
    }

    private int? ResolveCompletionTokens(string operationName)
    {
        var value = OptionalPositiveInt($"Azure:Operations:{operationName}:MaxCompletionTokens")
            ?? OptionalPositiveInt("Azure:Operations:Defaults:MaxCompletionTokens");
        return value;
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
        return value != null && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }
}
