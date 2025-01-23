using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.SettingManagement;

namespace Unity.GrantManager.Zones;

[Authorize]
public class ZoneManagementAppService(IZoneManager zoneManager) : GrantManagerAppService, IZoneManagementAppService
{
    /// <summary>
    /// Get flattened HashSet of zone names for simple zone state checks on pages.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public async Task<HashSet<string>> GetZoneStateSet(Guid formId)
    {
        var zoneTemplates = await zoneManager.GetAsync(formId);

        var enabledTabs = zoneTemplates.Zones
            .Where(zoneTab => zoneTab.IsEnabled)
            .Select(zoneTab => zoneTab.Name)
            .ToHashSet();

        var enabledZones = zoneTemplates.Zones
            .Where(zoneTab => zoneTab.IsEnabled)
            .SelectMany(zoneTab => zoneTab.Zones)
            .Where(zone => zone.IsEnabled)
            .Select(zone => zone.Name)
            .ToHashSet();

        enabledTabs.UnionWith(enabledZones);

        return enabledTabs;
    }
}
