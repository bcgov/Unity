using System.Collections.Generic;

namespace Unity.GrantManager.Zones;

public class ZoneGroupDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public List<ZoneTabDefinitionDto> Tabs { get; set; } = [];
}
