using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    public class AssessmentScoresWidgetViewModel
    {
        public Guid AssessmentId { get; set; }
        public int? FinancialAnalysis { get; set; }
        public int? EconomicImpact { get; set; }
        public int? InclusiveGrowth { get; set; } 
        public int? CleanGrowth { get; set; }
    }
}
