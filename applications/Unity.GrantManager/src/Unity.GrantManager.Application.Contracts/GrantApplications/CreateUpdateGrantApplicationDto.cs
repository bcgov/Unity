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
        public string? DueDilligenceStatus { get; set; }
        public string? Recommendation { get; set; }
        public string? DeclineRational { get; set; }
        public string? Notes { get; set; }
        public string? AssessmentResultStatus { get; set; }
    }
}
