using System;
using Unity.GrantManager.Attributes;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto
    {
        public Guid AssessmentId { get; set; }

        [MaxValue(int.MaxValue, "Financial Analysis")] 
        public int? FinancialAnalysis { get; set; }

        [MaxValue(int.MaxValue, "Economic Impact")] 
        public int? EconomicImpact { get; set; }

        [MaxValue(int.MaxValue, "Inclusive Growth")] 
        public int? InclusiveGrowth { get; set; }

        [MaxValue(int.MaxValue, "Clean Growth")] 
        public int? CleanGrowth { get; set; }
    }
}
