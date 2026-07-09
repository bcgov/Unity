using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.AI.Requests;

public class MappingSuggestionRequest
{
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }
}
