using System.Text.Json.Serialization;

namespace Unity.GrantManager.Integration.Sso
{
    public class UserSearchResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("err")]
        public string? Error { get; set; }

        [JsonPropertyName("data")]
        public SsoUser[]? Data { get; set; }
    }
}
