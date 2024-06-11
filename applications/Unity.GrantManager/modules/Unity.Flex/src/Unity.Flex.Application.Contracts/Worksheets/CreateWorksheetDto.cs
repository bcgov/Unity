using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public sealed class CreateWorksheetDto
    {
        public string Name { get; set; } = string.Empty;
        public string UIAnchor { get; set; } = string.Empty;
        public List<WorksheetSectionDto> Sections { get; set; } = [];
    }
}
