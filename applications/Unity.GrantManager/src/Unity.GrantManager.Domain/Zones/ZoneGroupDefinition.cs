using System.Collections.Generic;

namespace Unity.GrantManager.Zones;

public class ZoneGroupDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<ZoneTabDefinition> Tabs { get; set; } = [];
}
