using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public sealed class AttachmentSummaryBatchResponse
{
    [JsonPropertyName("attachments")]
    public List<AttachmentSummaryBatchItemResponse> Attachments { get; set; } = [];
}

public sealed class AttachmentSummaryBatchItemResponse
{
    [JsonPropertyName("attachmentId")]
    public string AttachmentId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
