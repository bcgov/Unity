using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CheckboxGroupDefinitionOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        
        [JsonPropertyName("value")]
        public bool Value { get; set; } = false;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }
}
