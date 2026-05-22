using System.Text.Json.Serialization;

namespace Unity.GrantManager.Applications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuditHistoryStatus
{
    InProgress = 1,
    Completed = 2
}
