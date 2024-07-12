using System;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateGrantApplicationDto
    {
        public string? ProjectSummary { get; set; }
        public int? TotalScore { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? TotalProjectBudget { get; set; }
        public decimal? RecommendedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string? LikelihoodOfFunding { get; set; }
        public string? DueDiligenceStatus { get; set; }
        public string? SubStatus { get; set; }
        public string? DeclineRational { get; set; }
        public string? Notes { get; set; }
        public string? AssessmentResultStatus { get; set; }
        public DateTime? FinalDecisionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Acquisition { get; set; }
        public string? Forestry { get; set; }
        public string? ForestryFocus { get; set; }
        public int? CommunityPopulation { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? ProjectEndDate { get; set; } 
        public string? RiskRanking { get; set;}
    }
}
