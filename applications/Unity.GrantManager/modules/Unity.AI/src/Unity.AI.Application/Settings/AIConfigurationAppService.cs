using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.AI.Permissions;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.AI.Settings;

public class AIConfigurationAppService : AIAppService, IAIConfigurationAppService
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly ICurrentTenant _currentTenant;

    public AIConfigurationAppService(
        ISettingProvider settingProvider,
        ISettingManager settingManager,
        ICurrentTenant currentTenant)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _currentTenant = currentTenant;
    }

    public virtual async Task<AIScoringSettingsDto> GetScoringSettingsAsync()
    {
        return new AIScoringSettingsDto
        {
            ScoringAssistantEnabled = await _settingProvider.GetAsync<bool>(
                AISettings.ScoringAssistantEnabled, defaultValue: false)
        };
    }

    [Authorize(AIPermissions.Configuration.ConfigureAI)]
    public virtual async Task UpdateScoringSettingsAsync(UpdateAIScoringSettingsDto input)
    {
        await _settingManager.SetAsync(
            AISettings.ScoringAssistantEnabled,
            input.ScoringAssistantEnabled.ToString().ToLowerInvariant(),
            TenantSettingValueProvider.ProviderName,
            _currentTenant.Id?.ToString());
    }
}
