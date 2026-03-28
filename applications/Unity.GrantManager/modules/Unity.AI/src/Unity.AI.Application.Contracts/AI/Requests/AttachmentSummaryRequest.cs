using System.Text.Json.Serialization;

namespace Unity.AI.Requests
{
    public class AttachmentSummaryRequest
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("fileContent")]
        public byte[] FileContent { get; set; } = System.Array.Empty<byte>();

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";

        [JsonPropertyName("promptVersion")]
        public string? PromptVersion { get; set; }
    }
}
