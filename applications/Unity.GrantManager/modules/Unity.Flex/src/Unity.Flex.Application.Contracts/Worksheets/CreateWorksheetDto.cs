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
        public string ReportColumns { get; set; } = string.Empty;
        public string ReportKeys { get; set; } = string.Empty;
        public string ReportViewName { get; set; } = string.Empty;
        public List<CreateWorksheetSectionDto> Sections { get; set; } = [];
    }
}
