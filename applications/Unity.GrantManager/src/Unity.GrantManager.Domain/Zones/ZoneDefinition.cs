using System.Text.Json.Serialization;

namespace Unity.GrantManager.Zones;

public class ZoneDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; } = 1000;

    [JsonIgnore]
    public bool IsConfigurationDisabled { get; set; } = false;
    [JsonIgnore]
    public string ViewComponentType { get; set; } = string.Empty;
    [JsonIgnore]
    public string? ElementId { get; set; }
}