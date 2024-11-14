using System;
using System.Text.Json.Serialization;
using Unity.Flex.Worksheets.Definitions.Interfaces;

namespace Unity.Flex.Worksheets.Definitions
{
    public class DateDefinition : CustomFieldDefinition, ICustomFieldFormat
    {
        [JsonPropertyName("min")]
        public DateTime Min { get; set; } = DateTime.MinValue;

        [JsonPropertyName("max")]
        public DateTime Max { get; set; } = DateTime.MaxValue;

        public string Format { get; set; } = string.Empty;

        public DateDefinition() : base()
        {
        }
    }
}
