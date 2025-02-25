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

    // TODO: How to handle scoped context for a single formId or providerKey
    // TODO: GetOrAdd
    // TODO: ConcurrentHashset

    public ZoneChecker(IZoneManagementAppService zoneManager)
    {
        _zoneManager = zoneManager;
    }

    public Task<HashSet<string>> GetOrNullAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsEnabledAsync(string name)
    {
        throw new NotImplementedException();
    }
}
