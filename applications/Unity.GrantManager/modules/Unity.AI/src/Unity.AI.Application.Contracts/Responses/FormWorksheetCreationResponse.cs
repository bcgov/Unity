using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormWorksheetCreationResponse
{
    [JsonPropertyName("worksheetName")]
    public string WorksheetName { get; set; } = string.Empty;

    [JsonPropertyName("suggestedFields")]
    public List<FormMappingFieldResponse> SuggestedFields { get; set; } = [];

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
