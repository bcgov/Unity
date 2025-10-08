using System.Text.Json.Serialization;

namespace Unity.Reporting.Configuration
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleStatus
    {
        NOTASSIGNED = 0,
        ASSIGNED = 1,
        FAILED = 2
    }
}
