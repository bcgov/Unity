using System.Collections.Generic;

namespace Unity.GrantManager.SettingManagement;

public class ZoneTabDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; } = 1000;
    public string? ElementId { get; set; }
    public List<ZoneDefinitionDto> Zones { get; set; } = [];
}
