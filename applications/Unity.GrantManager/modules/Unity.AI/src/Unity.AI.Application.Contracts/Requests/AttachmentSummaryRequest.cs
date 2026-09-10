using System.Text.Json.Serialization;

namespace Unity.AI.Requests
{
    public class AttachmentSummaryRequest
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";

        [JsonPropertyName("extractedText")]
        public string? ExtractedText { get; set; }

        [JsonPropertyName("promptVersion")]
        public string? PromptVersion { get; set; }
    }
}
