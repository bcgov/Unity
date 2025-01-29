using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.Zones;

[Authorize]
public class ZoneManagementAppService(IZoneManager zoneManager) : GrantManagerAppService, IZoneManagementAppService
{
    /// <summary>
    /// Get flattened HashSet of zone names for simple zone state checks on pages.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public async Task<HashSet<string>> GetZoneStateSetAsync(Guid formId)
    {
        var zoneTemplates = await zoneManager.GetAsync(formId.ToString());

        return zoneTemplates.Tabs
            .Where(zoneTab => zoneTab.IsEnabled)
            .SelectMany(zoneTab => new[] { zoneTab.Name }
            .Concat(zoneTab.Zones.Where(zone => zone.IsEnabled).Select(zone => zone.Name)))
            .ToHashSet();
    }

    public async Task<ZoneGroupDefinitionDto> GetAsync(string providerName, string providerKey)
    {
        var zoneTemplates = await zoneManager.GetAsync(providerName, providerKey);
        var updatedTemplate = UnmergeZoneConfigurationTemplate(zoneTemplates);
        return ObjectMapper.Map<ZoneGroupDefinition, ZoneGroupDefinitionDto>(updatedTemplate);
    }

    [Authorize(UnitySettingManagementPermissions.UserInterface)]
    public async Task SetAsync(string providerName, string providerKey, List<UpdateZoneDto> input)
    {
        if (Guid.TryParse(providerKey, out Guid providerId))
        {
            await zoneManager.SetForFormAsync(providerId, MergeZoneConfigurationTemplate(input));
        }
    }

    private static ZoneGroupDefinition UnmergeZoneConfigurationTemplate(ZoneGroupDefinition currentConfiguration)
    {
        return new ZoneGroupDefinition
        {
            Name = DefaultZoneDefinition.Template.Name,
            Tabs = DefaultZoneDefinition.Template.Tabs
                .Select(tab => new ZoneTabDefinition
                {
                    Name = tab.Name,
                    IsEnabled = currentConfiguration.Tabs.Any(templateTabs => templateTabs.Name == tab.Name && templateTabs.IsEnabled),
                    SortOrder = tab.SortOrder,
                    Zones = tab.Zones
                        .Select(zone => new ZoneDefinition
                        {
                            Name = zone.Name,
                            ViewComponentType = zone.ViewComponentType,
                            IsEnabled = zone.IsConfigurationDisabled
                                ? currentConfiguration.Tabs.Any(templateTabs => templateTabs.Name == tab.Name && templateTabs.IsEnabled)
                                : currentConfiguration.Tabs.SelectMany(et => et.Zones).Any(ez => ez.Name == zone.Name && ez.IsEnabled),
                            IsConfigurationDisabled = zone.IsConfigurationDisabled,
                            SortOrder = zone.SortOrder
                        })
                        .OrderBy(z => z.SortOrder)
                        .ToList()
                })
                .OrderBy(t => t.SortOrder)
                .ToList()
        };
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
}
