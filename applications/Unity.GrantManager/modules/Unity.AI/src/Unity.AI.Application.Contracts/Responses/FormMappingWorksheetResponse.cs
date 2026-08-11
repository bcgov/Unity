using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingWorksheetResponse
{
    [JsonPropertyName("worksheetName")]
    public string WorksheetName { get; set; } = string.Empty;

    [JsonPropertyName("fieldMatches")]
    public List<FormMappingMatchResponse> FieldMatches { get; set; } = [];
}
