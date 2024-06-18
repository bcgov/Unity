using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class CreateWorksheetSectionDto
    {
        public string Name { get; set; } = string.Empty;
        public uint Order { get; set; }
        public List<CreateCustomFieldDto> Fields { get; set; } = [];
    }
}
