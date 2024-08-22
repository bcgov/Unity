using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class QuestionYesNoDefinition : CustomFieldDefinition
    {
        public QuestionYesNoDefinition() : base()
        {
        }

        [JsonPropertyName("yes_value")]
        public long YesValue { get; set; } = 0;

        [JsonPropertyName("no_value")]
        public long NoValue { get; set; } = 0;
    }
}
