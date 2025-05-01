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
    /// <param name="formId">The formId to retrieve zone states for.</param>
    /// <returns>A HashSet containing the names of the zones in the specified form.</returns>
    public async Task<HashSet<string>> GetZoneStateSetAsync(Guid formId)
    {
        return await zoneManager.GetStateSetAsync("F", formId.ToString());
    }

    /// <summary>
    /// Retrieves a ZoneGroupDefinitionDto for the specified provider name and key.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="providerKey">The key of the provider.</param>
    /// <returns>A ZoneGroupDefinitionDto containing the zone group definition.</returns>
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

    /// <summary>
    /// Unmerges a ZoneGroupDefinition by resetting its configuration to the default template.
    /// Used on retrieving partial setting data to override the DefaultZoneDefinition with configured values.
    /// </summary>
    /// <param name="currentConfiguration">The current ZoneGroupDefinition to unmerge.</param>
    /// <returns>A new ZoneGroupDefinition based on the default template with updated states.</returns>
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
                            SortOrder = currentConfiguration.Tabs
                                .SelectMany(et => et.Zones)
                                .FirstOrDefault(ez => ez.Name == zone.Name)?.SortOrder ?? zone.SortOrder
                        })
                        .OrderBy(z => z.SortOrder)
                        .ToList()
                })
                .OrderBy(t => t.SortOrder)
                .ToList()
        };
    }

    /// <summary>
    /// Merges a list of UpdateZoneDto objects into a ZoneGroupDefinition.
    /// This method combines the provided zone updates with the default zone template,
    /// enabling or disabling zones and tabs based on the input.
    /// </summary>
    /// <param name="updateZoneDtos">A list of UpdateZoneDto objects containing the updated zone states.</param>
    /// <returns>A ZoneGroupDefinition object with the merged configuration.</returns>
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