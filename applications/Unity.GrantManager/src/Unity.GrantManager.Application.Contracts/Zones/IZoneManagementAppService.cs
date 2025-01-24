using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Zones;
public interface IZoneManagementAppService : IApplicationService
{
    Task<ZoneGroupDefinitionDto> GetAsync(string providerName, string providerKey);
    Task UpdateAsync(string providerName, string providerKey, List<UpdateZoneDto> input);
    Task<HashSet<string>> GetZoneStateSet(Guid formId);
    Task SetConfigurationAsync();
    Task<ApplicationUiSettingsDto> GetAsync();
    Task<ZoneGroupDefinitionDto> GetForFormAsync(Guid formId);
    Task SetConfigurationAsync(Guid formId, ZoneGroupDefinitionDto? input);
}
