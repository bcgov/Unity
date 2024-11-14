using System.Text.Json.Serialization;
using Unity.Flex.Worksheets.Definitions.Interfaces;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CurrencyDefinition : CustomFieldDefinition, ICustomFieldFormat
    {
        [JsonPropertyName("min")]
        public decimal Min { get; set; } = decimal.MinValue;

        [JsonPropertyName("max")]
        public decimal Max { get; set; } = decimal.MaxValue;

        public string Format { get; set; } = string.Empty;
    }
}
