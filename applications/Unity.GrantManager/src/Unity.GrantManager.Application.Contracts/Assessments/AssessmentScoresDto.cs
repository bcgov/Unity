using System;
using Unity.GrantManager.Attributes;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto
    {
        public Guid AssessmentId { get; set; }

        [MaxValue(int.MaxValue, "Section Score 1")]
        public int? FinancialAnalysis { get; set; }

        [MaxValue(int.MaxValue, "Section Score 2")]
        public int? EconomicImpact { get; set; }

        [MaxValue(int.MaxValue, "Section Score 3")]
        public int? InclusiveGrowth { get; set; }

        [MaxValue(int.MaxValue, "Section Score 4")]
        public int? CleanGrowth { get; set; }
    }
}
