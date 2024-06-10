using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Values
{
    public class CheckboxGroupValueOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public bool Value { get; set; }
    }
}
