using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class RadioDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("options")]
        public List<RadioOption> Options { get; set; } = [];

        public RadioDefinition() : base()
        {
        }
    }
}
