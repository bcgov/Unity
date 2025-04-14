using System.Collections.Generic;

namespace Unity.GrantManager.Zones;

public class ZoneTabDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; } = 1000;
    public string? ElementId { get; set; }
    public List<ZoneDefinitionDto> Zones { get; set; } = [];
}
