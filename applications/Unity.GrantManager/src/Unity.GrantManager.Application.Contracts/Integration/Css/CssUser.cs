using System.Text.Json.Serialization;

namespace Unity.GrantManager.Integration.Css
{
    public class CssUser
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("attributes")]
        public CssUserAttributes? Attributes { get; set; }
    }
}
