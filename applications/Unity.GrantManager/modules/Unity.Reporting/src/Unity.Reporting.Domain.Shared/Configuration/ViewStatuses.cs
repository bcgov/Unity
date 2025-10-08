using System.Text.Json.Serialization;

namespace Unity.Reporting.Configuration
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ViewStatus
    {
        GENERATING = 0,
        SUCCESS = 1,
        FAILED = 2
    }
}
