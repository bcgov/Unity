using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Zones;

namespace Unity.GrantManager.SettingManagement;

[Authorize]
public class ApplicationUiSettingsAppService(IZoneManager zoneManager) : GrantManagerAppService, IApplicationUiSettingsAppService
{
    public async Task<ZoneGroupDefinitionDto?> GetForFormAsync(Guid formId)
    {
        var zoneTemplates = await zoneManager.GetAsync(formId);
        var updatedTemplate = UnmergeZoneConfigurationTemplate(zoneTemplates);
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(updatedTemplate);
    }

    [Authorize(UnitySettingManagementPermissions.UserInterface)]
    public async Task UpdateAsync(string providerName, string providerKey, List<UpdateZoneDto> input)
    {
        var updatedTemplate = MergeZoneConfigurationTemplate(input);

        if (Guid.TryParse(providerKey, out Guid providerId))
        {
            await zoneManager.SetForFormAsync(providerId, updatedTemplate);
        }
    }

    private static ZoneGroupDefinition UnmergeZoneConfigurationTemplate(ZoneGroupDefinition existingTemplate)
    {
        var updatedZoneGroup = new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Tabs = DefaultZoneDefinition.Template.Tabs
                .Select(tab => new ZoneTabDefinition
                {
                    Name = tab.Name,
                    IsEnabled = existingTemplate.Tabs.Any(templateTabs => templateTabs.Name == tab.Name && templateTabs.IsEnabled),
                    SortOrder = tab.SortOrder,
                    Zones = tab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = (zone.IsConfigurationDisabled 
                                ? existingTemplate.Tabs.Any(templateTabs => templateTabs.Name == tab.Name && templateTabs.IsEnabled)
                                : existingTemplate.Tabs.SelectMany(et => et.Zones).Any(ez => ez.Name == zone.Name && ez.IsEnabled)),
                            IsConfigurationDisabled = zone.IsConfigurationDisabled,
                            SortOrder = zone.SortOrder
                        })
                        .OrderBy(z => z.SortOrder)
                        .ToList()
                })
                .OrderBy(t => t.SortOrder)
                .ToList()
        };

        return updatedZoneGroup;
    }

    public static ZoneGroupDefinition MergeZoneConfigurationTemplate(List<UpdateZoneDto> updateZoneDtos)
    {
        return new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Tabs = DefaultZoneDefinition.Template.Tabs
                .Select(tab => new ZoneTabDefinition
                {
                    Name = tab.Name,
                    IsEnabled = updateZoneDtos.Any(dto => dto.Name == tab.Name && dto.IsEnabled),
                    SortOrder = tab.SortOrder,
                    Zones = tab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = updateZoneDtos.Any(dto => dto.Name == zone.Name && dto.IsEnabled),
                            SortOrder = zone.SortOrder
                        })
                        .Where(zone => zone.IsEnabled && !zone.IsConfigurationDisabled) // Filter out disabled ZoneDefinitions
                        .ToList()
                })
                .Where(zoneTab => zoneTab.IsEnabled) // Filter out disabled ZoneTabDefinitions
                .ToList()
        };
    }

    public async Task<ZoneGroupDefinitionDto> GetTemplateAsync()
    {
        var zoneTemplates = await zoneManager.GetAsync();
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

    public Task<ApplicationUiSettingsDto> GetAsync()
    {
        throw new NotImplementedException();
    }
}
