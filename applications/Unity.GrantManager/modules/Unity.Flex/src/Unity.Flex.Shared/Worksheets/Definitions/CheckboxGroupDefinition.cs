using System.Collections.Generic;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CheckboxGroupDefinition : CustomFieldDefinition
    {
        public List<CheckboxOption> Options { get; set; } = [];
    }
}
