namespace Unity.GrantManager.Zones;

public class ZoneDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsConfigurationDisabled { get; set; }
    public string ViewComponentType { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1000;
    public string? ElementId { get; set; }
    public object? Arguments { get; set; }
}
