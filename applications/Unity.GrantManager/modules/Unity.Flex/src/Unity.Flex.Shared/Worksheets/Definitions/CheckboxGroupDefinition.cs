using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CheckboxGroupDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("options")]
        public List<CheckboxGroupDefinitionOption> Options { get; set; } = [];        
    }
}
