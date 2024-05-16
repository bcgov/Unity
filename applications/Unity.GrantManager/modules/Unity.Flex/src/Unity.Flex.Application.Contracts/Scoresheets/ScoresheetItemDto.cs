using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetItemDto
    {
        public string Type { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public int Order { get; set; }
        public Guid Scoresheetid { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
