using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Zones;

public interface IZoneManager
{
    Task<ZoneGroupDefinition> GetAsync();
    Task<ZoneGroupDefinition> GetAsync(string providerKey);
    Task<ZoneGroupDefinition> GetAsync(string providerName, string providerKey);
    Task SetForFormAsync(Guid formId, ZoneGroupDefinition template);
    Task SetForTennantAsync(ZoneGroupDefinition template);
}
