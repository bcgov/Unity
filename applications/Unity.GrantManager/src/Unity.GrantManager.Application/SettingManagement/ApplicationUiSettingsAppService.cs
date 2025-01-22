using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Settings;
using Unity.GrantManager.Zones;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.SettingManagement;

[Authorize]
public class ApplicationUiSettingsAppService(IZoneManager zoneManager) : GrantManagerAppService, IApplicationUiSettingsAppService
{
    public async Task<ZoneGroupDefinitionDto> GetTemplateAsync()
    {
        var zoneTemplates = await zoneManager.GetAsync();
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(zoneTemplates);
    }

    public async Task<ZoneGroupDefinitionDto?> GetForFormAsync(Guid formId)
    {
        var zoneTemplates = await zoneManager.GetAsync(formId);
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(zoneTemplates);
    }

    public async Task SetConfigurationAsync()
    {
        await zoneManager.SetForTennantAsync(DefaultZoneDefinition.Template);
    }

    public async Task SetConfigurationAsync(Guid formId, ZoneGroupDefinitionDto? input)
    {
        var submitTemplate = input != null ? ObjectMapper.Map<ZoneGroupDefinitionDto, ZoneGroupDefinition>(input) : DefaultZoneDefinition.Template;
        await zoneManager.SetForFormAsync(formId, submitTemplate);
    }

    // TODO: Deprecated, remove
    public async Task<ApplicationUiSettingsDto> GetAsync()
    {
        var settingsDto = new ApplicationUiSettingsDto
        {
            Submission = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Submission),
            Assessment = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Assessment),
            Project = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Project),
            Applicant = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Applicant),
            Payments = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Payments),
            FundingAgreement = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.FundingAgreement)
        };

        return settingsDto;
    }

    [Authorize(UnitySettingManagementPermissions.UserInterface)]
    public async Task UpdateAsync(string providerName, string providerKey, List<UpdateZoneDto> input)
    {
        var updatedTemplate = CreateZoneConfigurationFromTemplate(input);

        // Check if valid form
        // Check if zone valid for configured features

        if (Guid.TryParse(providerKey, out Guid providerId))
        {
            await zoneManager.SetForFormAsync(providerId, updatedTemplate);
        }
    }

    public static ZoneGroupDefinition CreateZoneConfigurationFromTemplate(List<UpdateZoneDto> updateZoneDtos)
    {
        return new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Zones = DefaultZoneDefinition.Template.Zones
                .Select(zoneTab => new ZoneTabDefinition
                {
                    Name = zoneTab.Name,
                    DisplayName = zoneTab.DisplayName,
                    IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zoneTab.Name)?.IsEnabled ?? zoneTab.IsEnabled,
                    SortOrder = zoneTab.SortOrder,
                    ElementId = zoneTab.ElementId,
                    Zones = zoneTab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zone.Name)?.IsEnabled ?? zone.IsEnabled,
                            SortOrder = zone.SortOrder,
                            DisplayName = zone.DisplayName
                        })
                        .Where(zone => zone.IsEnabled) // Filter out disabled ZoneDefinitions
                        .ToList()
                })
                .Where(zoneTab => zoneTab.IsEnabled) // Filter out disabled ZoneTabDefinitions
                .ToList()
        };
    }
}
