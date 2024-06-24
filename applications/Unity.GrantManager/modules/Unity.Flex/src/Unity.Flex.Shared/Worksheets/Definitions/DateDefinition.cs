using System;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class DateDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("min")]
        public DateTime Min { get; set; } = DateTime.MinValue;

        [JsonPropertyName("max")]
        public DateTime Max { get; set; } = DateTime.MaxValue;

        public DateDefinition() : base()
        {            
        }
    }
}
