using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingResponse
{
    public string Mapping { get; set; } = string.Empty;

    [JsonPropertyName("coreFieldMatches")]
    public List<FormMappingMatchResponse> CoreFieldMatches { get; set; } = [];

    [JsonPropertyName("issues")]
    public List<FormMappingIssueResponse> Issues { get; set; } = [];
}
