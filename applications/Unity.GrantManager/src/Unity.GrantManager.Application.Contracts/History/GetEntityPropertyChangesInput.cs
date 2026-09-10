using System;

namespace Unity.GrantManager.History;

public class GetEntityPropertyChangesInput
{
    public string? EntityId { get; set; }
    public string? EntityTypeFullName { get; set; }
    public string[] PropertyNames { get; set; } = [];
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int MaxResultCount { get; set; } = 50;
    public int SkipCount { get; set; }
}
