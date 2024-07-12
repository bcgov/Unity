using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class NumericDefinition : CustomFieldDefinition
    {
        public NumericDefinition() : base()
        {
        }

        [JsonPropertyName("min")]
        public long Min { get; set; } = long.MinValue;

        [JsonPropertyName("max")]
        public long Max { get; set; } = long.MaxValue;
    }
}
