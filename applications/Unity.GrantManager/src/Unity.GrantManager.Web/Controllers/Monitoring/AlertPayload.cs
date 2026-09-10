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

    private List<AlertItem> _alerts = [];

    [JsonPropertyName("alerts")]
    public List<AlertItem> Alerts
    {
        get => _alerts;
        set => _alerts = value ?? [];
    }
}

public class AlertItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    private Dictionary<string, string> _labels = [];
    private Dictionary<string, string> _annotations = [];

    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels
    {
        get => _labels;
        set => _labels = value ?? [];
    }

    [JsonPropertyName("annotations")]
    public Dictionary<string, string> Annotations
    {
        get => _annotations;
        set => _annotations = value ?? [];
    }

    [JsonPropertyName("startsAt")]
    public DateTimeOffset StartsAt { get; set; }

    [JsonPropertyName("generatorURL")]
    public string GeneratorURL { get; set; } = string.Empty;

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;
}
