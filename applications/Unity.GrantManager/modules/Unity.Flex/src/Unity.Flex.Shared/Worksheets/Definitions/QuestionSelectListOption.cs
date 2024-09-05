using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class QuestionSelectListOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("numeric_value")]
        public long NumericValue { get; set; } = 0;
    }
}
