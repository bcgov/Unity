using System.Collections.Generic;

namespace Unity.Flex.Worksheets.Definitions
{
    public class SelectListDefinition : CustomFieldDefinition
    {
        public List<SelectListOption> Options { get; set; } = [];
    }
}
