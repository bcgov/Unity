using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class CreateSectionDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid ScoresheetId { get; set; }
    }
}
