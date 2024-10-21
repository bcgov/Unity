using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CustomFieldDefinition
    {
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        public CustomFieldDefinition() : base()
        {
        }
    }
}
