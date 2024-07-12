using System;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateAssessmentResultsDto : CustomDataFieldDto
    {
        public DateTime? DueDate { get; set; }
        public DateTime? NotificationDate { get; set; }
        public string? Notes { get; set; }
        public string? SubStatus { get; set; }
        public string? LikelihoodOfFunding { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal? RequestedAmount { get; set; }
        public int? TotalScore { get; set; }
        public DateTime? FinalDecisionDate { get; set; }
        public string? ProjectSummary { get; set; }
        public string? DueDiligenceStatus { get; set; }
        public decimal? TotalProjectBudget { get; set; }
        public decimal? RecommendedAmount { get; set; }
        public string? DeclineRational { get; set; }
        public string? AssessmentResultStatus { get; set; }
        public string? RiskRanking { get; set;} 
    }
}
