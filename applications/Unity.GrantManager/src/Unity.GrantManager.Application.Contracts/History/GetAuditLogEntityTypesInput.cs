using System;

namespace Unity.GrantManager.History;

public class GetAuditLogEntityTypesInput
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}