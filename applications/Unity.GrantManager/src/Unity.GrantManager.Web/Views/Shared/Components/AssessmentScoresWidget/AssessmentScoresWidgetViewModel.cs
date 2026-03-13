using System;
using System.ComponentModel.DataAnnotations;
using Unity.Flex.Scoresheets;
using Unity.GrantManager.Assessments;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    public class AssessmentScoresWidgetViewModel
    {
        public Guid AssessmentId { get; set; }
        
        [Range(0, 99)]
        public int? FinancialAnalysis { get; set; }

        [Range(0, 99)]
        public int? EconomicImpact { get; set; }

        [Range(0, 99)]
        public int? InclusiveGrowth { get; set; } 
        
        [Range(0, 99)]
        public int? CleanGrowth { get; set; }
        public AssessmentState? Status { get; set; }

        public Guid CurrentUserId { get; set; }
        public Guid AssessorId { get; set; }
        public ScoresheetDto? Scoresheet { get; set; }
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
