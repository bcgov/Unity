using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Zones;

public interface IZoneManager
{
    Task<ZoneGroupDefinition> GetAsync();
    Task<ZoneGroupDefinition> GetAsync(Guid formId);
    Task SetForFormAsync(Guid formId, ZoneGroupDefinition template);
    Task SetForTennantAsync(ZoneGroupDefinition template);
}
