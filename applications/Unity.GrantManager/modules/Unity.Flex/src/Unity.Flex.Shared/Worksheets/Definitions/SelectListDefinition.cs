using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class SelectListDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("options")]
        public List<SelectListOption> Options { get; set; } = [];

        public SelectListDefinition() : base()
        {           
        }
    }
}
