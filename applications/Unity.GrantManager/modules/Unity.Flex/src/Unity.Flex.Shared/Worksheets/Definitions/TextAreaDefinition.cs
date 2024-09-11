using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class TextAreaDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("minLength")]
        public uint MinLength { get; set; }

        [JsonPropertyName("maxLength")]
        public uint MaxLength { get; set; } = uint.MaxValue;

        [JsonProperty("rows")]
        public uint Rows { get; set; } = uint.MinValue;

        public TextAreaDefinition() : base()
        {
        }
    }
}
