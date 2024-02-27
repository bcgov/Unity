using System;
using Unity.GrantManager.Assessments;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    public class AssessmentScoresWidgetViewModel
    {
        public Guid AssessmentId { get; set; }
        public int? FinancialAnalysis { get; set; }
        public int? EconomicImpact { get; set; }
        public int? InclusiveGrowth { get; set; } 
        public int? CleanGrowth { get; set; }
        public AssessmentState? Status { get; set; }

        public Guid CurrentUserId { get; set; }
        public Guid AssessorId { get; set; }

        public bool IsDisabled()
        {
            if(CurrentUserId != AssessorId)
            {
                return true;
            } 
            else if (Status.Equals(AssessmentState.COMPLETED))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int? ScoreTotal()
        {
            return FinancialAnalysis + EconomicImpact + InclusiveGrowth + CleanGrowth;
        }
    }
}
