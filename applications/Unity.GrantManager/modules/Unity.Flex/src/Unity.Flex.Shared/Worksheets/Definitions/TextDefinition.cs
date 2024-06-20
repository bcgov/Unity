using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class TextDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("minLength")]
        public uint MinLength { get; set; }

        [JsonPropertyName("maxLength")]
        public uint MaxLength { get; set; } = uint.MaxValue;

        public TextDefinition() : base()
        {
        }
    }
}
