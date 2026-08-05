using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingMatchResponse
{
    [JsonPropertyName("sourceField")]
    public string SourceField { get; set; } = string.Empty;

    [JsonPropertyName("targetField")]
    public string TargetField { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }
}
