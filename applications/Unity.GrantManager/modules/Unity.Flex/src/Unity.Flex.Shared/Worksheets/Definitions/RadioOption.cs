using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class RadioOption
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }
}
