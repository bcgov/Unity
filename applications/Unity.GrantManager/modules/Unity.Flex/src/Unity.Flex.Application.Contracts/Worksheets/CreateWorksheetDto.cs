using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public sealed class CreateWorksheetDto
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;        
        public List<CreateWorksheetSectionDto> Sections { get; set; } = [];
    }
}
