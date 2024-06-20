using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CurrencyDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("min")]
        public decimal Min { get; set; } = decimal.MinValue;

        [JsonPropertyName("max")]
        public decimal Max { get; set; } = decimal.MaxValue;
    }
}
