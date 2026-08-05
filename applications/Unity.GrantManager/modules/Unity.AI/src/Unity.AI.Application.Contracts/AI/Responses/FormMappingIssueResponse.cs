using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormMappingIssueResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
