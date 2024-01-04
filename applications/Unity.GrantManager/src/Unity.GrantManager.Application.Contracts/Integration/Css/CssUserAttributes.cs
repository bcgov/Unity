using System.Text.Json.Serialization;

namespace Unity.GrantManager.Integration.Css
{
    public class CssUserAttributes
    {
        [JsonPropertyName("idir_user_guid")]
        public string[]? IdirUserGuid { get; set; }

        [JsonPropertyName("idir_username")]
        public string[]? IdirUsername { get; set; }

        [JsonPropertyName("display_name")]
        public string[]? DisplayName { get; set; }
    }
}
