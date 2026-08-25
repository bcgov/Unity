using System.Text.Json.Serialization;

namespace Unity.GrantManager.Logs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExceptionLogSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
