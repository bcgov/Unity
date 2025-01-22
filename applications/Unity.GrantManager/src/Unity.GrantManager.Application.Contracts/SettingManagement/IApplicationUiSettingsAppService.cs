using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.SettingManagement;
public interface IApplicationUiSettingsAppService : IApplicationService
{
    Task<ApplicationUiSettingsDto> GetAsync();
    Task<ZoneGroupDefinitionDto?> GetForFormAsync(Guid formId);
    Task UpdateAsync(string providerName, string providerKey, List<UpdateZoneDto> input);
    Task SetConfigurationAsync();
    Task SetConfigurationAsync(Guid formId, ZoneGroupDefinitionDto? input);
}
