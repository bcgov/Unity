using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public sealed class ApplicationScoringBatchResponse
{
    [JsonPropertyName("sections")]
    public List<ApplicationScoringBatchSectionResponse> Sections { get; set; } = [];
}

public sealed class ApplicationScoringBatchSectionResponse
{
    [JsonPropertyName("sectionId")]
    public string SectionId { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("answers")]
    public List<ApplicationScoringBatchAnswerResponse> Answers { get; set; } = [];
}

public sealed class ApplicationScoringBatchAnswerResponse
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
