using System.Collections.Generic;

namespace Unity.GrantManager.SettingManagement;

public class ZoneGroupDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public List<ZoneTabDefinitionDto> Zones { get; set; } = [];
}
