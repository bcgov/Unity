using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.Web.Controllers.Monitoring;

public class AlertManagerPayload
{
    [JsonPropertyName("receiver")]
    public string Receiver { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("alerts")]
    public List<AlertItem> Alerts { get; set; } = [];
}

public class AlertItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels { get; set; } = [];

    [JsonPropertyName("annotations")]
    public Dictionary<string, string> Annotations { get; set; } = [];

    [JsonPropertyName("startsAt")]
    public DateTimeOffset StartsAt { get; set; }

    [JsonPropertyName("generatorURL")]
    public string GeneratorURL { get; set; } = string.Empty;

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;
}
