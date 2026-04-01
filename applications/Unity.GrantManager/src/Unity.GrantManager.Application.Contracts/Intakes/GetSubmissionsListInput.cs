using System;

namespace Unity.GrantManager.Intakes;

[Serializable]
public class GetSubmissionsListInput
{
    public bool ReturnAllSubmissions { get; set; } = true;

    public string? TenantName { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
