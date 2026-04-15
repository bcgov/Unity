using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.AI.Permissions;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.AI.Settings;

public class AIConfigurationAppService(
    ISettingProvider settingProvider,
    ISettingManager settingManager,
    ICurrentTenant currentTenant) : AIAppService, IAIConfigurationAppService
{
    private readonly ISettingProvider _settingProvider = settingProvider;
    private readonly ISettingManager _settingManager = settingManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public virtual async Task<AITenantConfigurationDto> GetTenantConfigurationAsync()
    {
        return new AITenantConfigurationDto
        {
            AutomaticGenerationEnabled = await _settingProvider.GetAsync<bool>(
                AISettings.AutomaticGenerationEnabled, defaultValue: false),
            ManualGenerationEnabled = await _settingProvider.GetAsync<bool>(
                AISettings.ManualGenerationEnabled, defaultValue: false)
        };
    }

    [Authorize(AIPermissions.Configuration.ConfigureAI)]
    public virtual async Task UpdateTenantConfigurationAsync(UpdateAITenantConfigurationDto input)
    {
        await _settingManager.SetAsync(
            AISettings.AutomaticGenerationEnabled,
            input.AutomaticGenerationEnabled.ToString().ToLowerInvariant(),
            TenantSettingValueProvider.ProviderName,
            _currentTenant.Id?.ToString());

        await _settingManager.SetAsync(
            AISettings.ManualGenerationEnabled,
            input.ManualGenerationEnabled.ToString().ToLowerInvariant(),
            TenantSettingValueProvider.ProviderName,
            _currentTenant.Id?.ToString());
    }
}
