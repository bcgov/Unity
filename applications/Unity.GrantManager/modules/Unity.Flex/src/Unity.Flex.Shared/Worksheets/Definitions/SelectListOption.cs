using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class SelectListOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
