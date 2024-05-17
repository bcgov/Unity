using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class EditSectionDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid SectionId { get; set; }
    }
}
