using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Services;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Zones;

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

    public Task<ZoneGroupDefinition> GetAsync()
    {
        return Task.FromResult(DefaultZoneDefinition.Template);
    }

    public async Task<ZoneGroupDefinition> GetAsync(string providerKey)
    {
        return await GetAsync(FormProviderKey, providerKey);
    }

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

    public async Task SetForFormAsync(Guid formId, ZoneGroupDefinition template)
    {
        var jsonTemplate = SerializeTemplate(template);
        await _settingManager.SetAsync(SettingsConstants.UI.Zones, jsonTemplate, FormProviderKey, formId.ToString());
    }

    public async Task SetForTennantAsync(ZoneGroupDefinition template)
    {
        var jsonTemplate = SerializeTemplate(template);
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Zones, jsonTemplate);
    }

    private string SerializeTemplate(ZoneGroupDefinition template)
    {
        return JsonSerializer.Serialize(template, _jsonSerializerOptions);
    }
}
