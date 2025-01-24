using System.Text.Json.Serialization;

namespace Unity.GrantManager.Zones;

public class ZoneDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    [JsonIgnore]
    public bool IsConfigurationDisabled { get; set; } = false;
    public string ViewComponentType { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1000;
    public string? ElementId { get; set; }
}