using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class QuestionSelectListDefinition : CustomFieldDefinition
    {
        [JsonPropertyName("options")]
        public List<QuestionSelectListOption> Options { get; set; } = [];

        public QuestionSelectListDefinition() : base()
        {           
        }
    }
}
