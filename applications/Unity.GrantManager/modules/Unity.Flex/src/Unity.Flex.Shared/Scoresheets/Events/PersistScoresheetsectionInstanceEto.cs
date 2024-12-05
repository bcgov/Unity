using System;
using System.Collections.Generic;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class PersistScoresheetSectionInstanceEto
    {
        public Guid AssessmentId { get; set; }
        public List<AssessmentAnswersEto> AssessmentAnswers { get; set; } = [];
    }
}
