using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class RadioDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("options")]
        public List<RadioOption> Options { get; set; } = [];
        [JsonPropertyName("label")]
        public string GroupLabel { get; set; } = string.Empty;

        public RadioDefinition() : base()
        {
        }
    }
}
