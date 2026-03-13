using System;
using Newtonsoft.Json;

namespace Unity.GrantManager.GrantsPortal.Messages;

public class MessageAcknowledgment
{
    [JsonProperty("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("messageType")]
    public string MessageType { get; set; } = "MessageAcknowledgment";

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonProperty("pluginId")]
    public string PluginId { get; set; } = "UNITY";

    [JsonProperty("originalMessageId")]
    public string OriginalMessageId { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("details")]
    public string Details { get; set; } = string.Empty;

    [JsonProperty("processedAt")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
