using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Zones;
public class ZoneChecker : IZoneChecker, IScopedDependency
{
    private IZoneManagementAppService _zoneManager { get; set; }
    public Guid FormId { get; set; }
    public HashSet<string> ZoneGrants { get; } = [];
    private bool _isZoneGrantsLoaded;
    private Guid _lastLoadedFormId = Guid.Empty;

    public ZoneChecker(IZoneManagementAppService zoneManager)
    {
        _zoneManager = zoneManager;
    }

    public async Task<HashSet<string>> GetOrNullAsync(string name)
    {
        await EnsureZoneGrantsLoadedAsync();
        return ZoneGrants.Contains(name) ? ZoneGrants : null;
    }

    public async Task<bool> IsEnabledAsync(string name)
    {
        await EnsureZoneGrantsLoadedAsync();
        return ZoneGrants.Contains(name);
    }

    private async Task EnsureZoneGrantsLoadedAsync()
    {
        if (FormId == Guid.Empty)
            return;

        // Reload zone grants if this is the first load or if FormId has changed
        if (!_isZoneGrantsLoaded || _lastLoadedFormId != FormId)
        {
            var zoneGrants = await _zoneManager.GetZoneStateSetAsync(FormId);
            ZoneGrants.Clear();
            foreach (var zone in zoneGrants)
            {
                ZoneGrants.Add(zone);
            }
            _isZoneGrantsLoaded = true;
            _lastLoadedFormId = FormId;
        }
    }
}
