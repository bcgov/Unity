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
    public async Task<ZoneGroupDefinitionDto> GetTemplateAsync()
    {
        var zoneTemplates = await zoneManager.GetAsync();
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(zoneTemplates);
    }

    public async Task<ZoneGroupDefinitionDto?> GetForFormAsync(Guid formId)
    {
        var zoneTemplates = await zoneManager.GetAsync(formId);
        var updatedTemplate = ReverseCreateZoneConfigurationFromTemplate(zoneTemplates);
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(updatedTemplate);
    }

    private static ZoneGroupDefinition ReverseCreateZoneConfigurationFromTemplate(ZoneGroupDefinition existingTemplate)
    {
        var updatedZoneGroup = new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Zones = DefaultZoneDefinition.Template.Zones
                .Select(zoneTab => new ZoneTabDefinition
                {
                    Name = zoneTab.Name,
                    IsEnabled = existingTemplate.Zones.FirstOrDefault(et => et.Name == zoneTab.Name)?.IsEnabled ?? false,
                    SortOrder = zoneTab.SortOrder,
                    Zones = zoneTab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = existingTemplate.Zones
                                .SelectMany(et => et.Zones)
                                .FirstOrDefault(ez => ez.Name == zone.Name)?.IsEnabled ?? false,
                            SortOrder = zone.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        };

        return updatedZoneGroup;
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

    //public async Task GetAsync(string providerName, string providerKey))
    //{
    //    if (Guid.TryParse())
    //    {
    //        var zoneTemplates = await zoneManager.GetAsync(formId);
    //    }
    //    return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(zoneTemplates);
    //}

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

    public static ZoneGroupDefinition CreateSettingPageTemplate(List<UpdateZoneDto> updateZoneDtos)
    {
        return new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Zones = DefaultZoneDefinition.Template.Zones
                .Select(zoneTab => new ZoneTabDefinition
                {
                    Name = zoneTab.Name,
                    IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zoneTab.Name)?.IsEnabled ?? zoneTab.IsEnabled,
                    SortOrder = zoneTab.SortOrder,
                    Zones = zoneTab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zone.Name)?.IsEnabled ?? zone.IsEnabled,
                            SortOrder = zone.SortOrder
                        })
                        .Where(zone => zone.IsEnabled) // Filter out disabled ZoneDefinitions
                        .ToList()
                })
                .Where(zoneTab => zoneTab.IsEnabled) // Filter out disabled ZoneTabDefinitions
                .ToList()
        };
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
                    IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zoneTab.Name)?.IsEnabled ?? zoneTab.IsEnabled,
                    SortOrder = zoneTab.SortOrder,
                    Zones = zoneTab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = updateZoneDtos.FirstOrDefault(dto => dto.Name == zone.Name)?.IsEnabled ?? zone.IsEnabled,
                            SortOrder = zone.SortOrder
                        })
                        .Where(zone => zone.IsEnabled) // Filter out disabled ZoneDefinitions
                        .ToList()
                })
                .Where(zoneTab => zoneTab.IsEnabled) // Filter out disabled ZoneTabDefinitions
                .ToList()
        };
    }

    public Task<ApplicationUiSettingsDto> GetAsync()
    {
        throw new NotImplementedException();
    }
}
