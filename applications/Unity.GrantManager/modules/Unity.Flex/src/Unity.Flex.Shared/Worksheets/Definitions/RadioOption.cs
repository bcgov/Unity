using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class RadioOption
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        public RadioOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
