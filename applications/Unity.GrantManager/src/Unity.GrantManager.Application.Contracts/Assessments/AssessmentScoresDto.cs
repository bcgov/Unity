using System;
using Unity.GrantManager.Attributes;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto
    {
        public Guid AssessmentId { get; set; }

        [MaxValue(int.MaxValue, "Section Score 1")] // We're renaming this 
        public int? FinancialAnalysis { get; set; }

        [MaxValue(int.MaxValue, "Section Score 2")] // We're renaming this
        public int? EconomicImpact { get; set; }

        [MaxValue(int.MaxValue, "Section Score 3")] // We're renaming this
        public int? InclusiveGrowth { get; set; }

        [MaxValue(int.MaxValue, "Section Score 4")] // We're renaming this
        public int? CleanGrowth { get; set; }
    }
}
