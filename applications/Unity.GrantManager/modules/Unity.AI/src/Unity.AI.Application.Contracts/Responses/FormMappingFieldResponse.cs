using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingFieldResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("isCustom")]
    public bool IsCustom { get; set; }
}
