namespace Unity.GrantManager.Zones;
public class UpdateZoneDto
{
    public required string Name { get; set; }
    public bool IsEnabled { get; set; }
    public int? SortOrder { get; set; }
}
