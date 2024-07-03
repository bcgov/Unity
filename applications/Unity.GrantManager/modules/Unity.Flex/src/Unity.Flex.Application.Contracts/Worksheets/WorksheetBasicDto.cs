using System;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetBasicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public uint TotalFields { get; set; } = 0;
        public uint TotalSections { get; set; } = 0;
        public uint Version { get; set; } = 0;
        public bool Published { get; set; } = false;
    }
}
