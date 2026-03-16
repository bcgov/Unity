using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.GrantsPortal.Messages;

public class PluginDataEnvelope
{
    [JsonProperty("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonProperty("messageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonProperty("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonProperty("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonProperty("data")]
    public JObject? Data { get; set; }
}
