using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class MappingSuggestionResponse
{
    [JsonPropertyName("coreFieldMatches")]
    public List<MappingSuggestionItemResponse> CoreFieldMatches { get; set; } = [];

    [JsonPropertyName("worksheetMatches")]
    public List<WorksheetMappingSuggestionResponse> WorksheetMatches { get; set; } = [];

    [JsonPropertyName("worksheetCreationSuggestions")]
    public List<WorksheetCreationSuggestionResponse> WorksheetCreationSuggestions { get; set; } = [];

    [JsonPropertyName("issues")]
    public List<MappingIssueResponse> Issues { get; set; } = [];
}

public class MappingSuggestionItemResponse
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

public class WorksheetMappingSuggestionResponse
{
    [JsonPropertyName("worksheetName")]
    public string WorksheetName { get; set; } = string.Empty;

    [JsonPropertyName("fieldMatches")]
    public List<MappingSuggestionItemResponse> FieldMatches { get; set; } = [];
}

public class WorksheetCreationSuggestionResponse
{
    [JsonPropertyName("worksheetName")]
    public string WorksheetName { get; set; } = string.Empty;

    [JsonPropertyName("suggestedFields")]
    public List<MappingFieldResponse> SuggestedFields { get; set; } = [];

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class MappingFieldResponse
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

public class MappingIssueResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
