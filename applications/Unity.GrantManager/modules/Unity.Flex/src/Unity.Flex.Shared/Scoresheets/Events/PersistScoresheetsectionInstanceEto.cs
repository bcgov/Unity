using System;
using System.Collections.Generic;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class PersistScoresheetSectionInstanceEto
    {
        public Guid SectionId { get; set; }
        public List<AssessmentAnswersEto> AssessmentAnswers { get; set; } = [];
    }
}
