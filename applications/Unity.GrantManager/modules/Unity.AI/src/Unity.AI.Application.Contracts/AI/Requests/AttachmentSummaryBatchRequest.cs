using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Requests;

public sealed class AttachmentSummaryBatchRequest
{
    [JsonPropertyName("attachments")]
    public List<AttachmentSummaryBatchItemRequest> Attachments { get; set; } = [];

    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }
}

public sealed class AttachmentSummaryBatchItemRequest
{
    [JsonPropertyName("attachmentId")]
    public string AttachmentId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "application/octet-stream";

    [JsonPropertyName("extractedText")]
    public string? ExtractedText { get; set; }
}
