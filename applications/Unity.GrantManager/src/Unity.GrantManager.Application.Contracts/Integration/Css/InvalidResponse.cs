using System.Text.Json.Serialization;

namespace Unity.GrantManager.Integrations.Css
{
    public class InvalidResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public object[]? Data { get; set; }

        [JsonPropertyName("err")]
        public string? Error { get; set; }
    }
}
