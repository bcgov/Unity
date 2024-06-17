using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ClonedObjectDto
    {
        public Guid ScoresheetId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? QuestionId { get; set; }
    }
}
