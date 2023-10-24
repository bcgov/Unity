using System;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto
    {
        public Guid AssessmentId { get; set; }
        public int? FinancialAnalysis { get; set; }
        public int? EconomicImpact { get; set; }
        public int? InclusiveGrowth { get; set; } 
        public int? CleanGrowth { get; set; }
    }
}
