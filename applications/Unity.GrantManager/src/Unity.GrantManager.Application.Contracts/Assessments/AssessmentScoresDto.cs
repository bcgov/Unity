using System;
using Unity.GrantManager.Attributes;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto
    {
        public Guid AssessmentId { get; set; }

        [MaxValue(int.MaxValue, "Section Score 1")] 
        public int? SectionScore1 { get; set; }

        [MaxValue(int.MaxValue, "Section Score 2")] 
        public int? SectionScore2 { get; set; }

        [MaxValue(int.MaxValue, "Section Score 3")] 
        public int? SectionScore3 { get; set; }

        [MaxValue(int.MaxValue, "Section Score 4")] 
        public int? SectionScore4 { get; set; }
    }
}
