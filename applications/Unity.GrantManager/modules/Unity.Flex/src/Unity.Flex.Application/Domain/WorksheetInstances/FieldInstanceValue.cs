using System.Text.Json.Serialization;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class FieldInstanceValue
    {
        public FieldInstanceValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
