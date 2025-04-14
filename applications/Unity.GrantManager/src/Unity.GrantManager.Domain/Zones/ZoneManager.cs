using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Services;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Zones;

/// <summary>
/// Manages zone group definitions and their configurations for forms and tenants. Used for dynamic UI configuration.
/// </summary>
public class ZoneManager : DomainService, IZoneManager, ITransientDependency
{
    private const string FormProviderKey = "F";
    private readonly ISettingManager _settingManager;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ZoneManager(ISettingManager settingManager)
    {
        _settingManager = settingManager;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Retrieves the default zone group definition template.
    /// </summary>
    /// <returns>The default <see cref="ZoneGroupDefinition"/> template.</returns>
    public Task<ZoneGroupDefinition> GetAsync()
    {
        return Task.FromResult(DefaultZoneDefinition.Template);
    }

    /// <summary>
    /// Retrieves the zone group definition for a specific form id.
    /// </summary>
    /// <param name="providerKey">The unique key identifying the form.</param>
    /// <returns>The zone group definition, or the default template if no configuration is found.</returns>
    public async Task<ZoneGroupDefinition> GetAsync(string providerKey)
    {
        return await GetAsync(FormProviderKey, providerKey);
    }

    /// <summary>
    /// Retrieves the zone group definition for a specific provider name and key.
    /// </summary>
    /// <param name="providerName">The name of the provider (e.g., tenant or form).</param>
    /// <param name="providerKey">The unique key identifying the provider.</param>
    /// <returns>The zone group definition, or the default template if no configuration is found.</returns>
    public async Task<ZoneGroupDefinition> GetAsync(string providerName, string providerKey)
    {
        var configurationJson = await _settingManager.GetOrNullAsync(SettingsConstants.UI.Zones, providerName, providerKey, fallback: true);
        ZoneGroupDefinition? currentConfiguration = null;
        if (configurationJson != null)
        {
            currentConfiguration = JsonSerializer.Deserialize<ZoneGroupDefinition>(configurationJson);
        }
        return currentConfiguration ?? DefaultZoneDefinition.Template;
    }

    /// <summary>
    /// Retrieves a set of enabled zone names from the zone configuration based on the provider name and key.
    /// </summary>
    /// <param name="providerName">The name of the provider (e.g., tenant or form).</param>
    /// <param name="providerKey">The unique key identifying the provider.</param>
    /// <returns>A HashSet containing the names of enabled tabs and zones.</returns>
    public async Task<HashSet<string>> GetStateSetAsync(string providerName, string providerKey)
    {
        var zoneTemplates = await GetAsync(providerName, providerKey);

        return zoneTemplates.Tabs
            .Where(zoneTab => zoneTab.IsEnabled)
            .SelectMany(zoneTab => new[] { zoneTab.Name }
            .Concat(zoneTab.Zones.Where(zone => zone.IsEnabled).Select(zone => zone.Name)))
            .ToHashSet();
    }

    /// <summary>
    /// Sets the zone group definition for a specific form.
    /// </summary>
    /// <param name="formId">The unique identifier of the form.</param>
    /// <param name="template">The zone group definition to be set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetForFormAsync(Guid formId, ZoneGroupDefinition template)
    {
        var jsonTemplate = JsonSerializer.Serialize(template, _jsonSerializerOptions);
        await _settingManager.SetAsync(SettingsConstants.UI.Zones, jsonTemplate, FormProviderKey, formId.ToString());
    }

    /// <summary>
    /// Sets the zone group definition for the current tenant.
    /// </summary>
    /// <param name="template">The zone group definition to be set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetForTennantAsync(ZoneGroupDefinition template)
    {
        var jsonTemplate = JsonSerializer.Serialize(template, _jsonSerializerOptions);
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Zones, jsonTemplate);
    }
}
