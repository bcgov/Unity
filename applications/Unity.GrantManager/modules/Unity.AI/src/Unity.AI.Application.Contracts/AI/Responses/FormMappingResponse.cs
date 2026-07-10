using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingResponse
{
    [JsonPropertyName("coreFieldMatches")]
    public List<FormMappingMatchResponse> CoreFieldMatches { get; set; } = [];

    [JsonPropertyName("worksheetMatches")]
    public List<FormMappingWorksheetResponse> WorksheetMatches { get; set; } = [];

    [JsonPropertyName("worksheetCreationSuggestions")]
    public List<FormWorksheetCreationResponse> WorksheetCreationSuggestions { get; set; } = [];

    [JsonPropertyName("issues")]
    public List<FormMappingIssueResponse> Issues { get; set; } = [];
}
