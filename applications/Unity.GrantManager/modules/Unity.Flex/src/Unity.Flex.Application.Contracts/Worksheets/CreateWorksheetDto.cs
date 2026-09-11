using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public sealed class CreateWorksheetDto
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;        
        public uint Version { get; set; } = 1;
        public bool Published { get; set; } = false;
        public List<CreateWorksheetSectionDto> Sections { get; set; } = [];
    }
}
