using System;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateProjectInfoDto
    {
        public string? ProjectSummary { get; set; }
        public string? ProjectName { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? TotalProjectBudget { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? ProjectEndDate { get; set; }
        public float? PercentageTotalProjectBudget { get; set; }
        public double? ProjectFundingTotal { get; set; }
        public string? Community { get; set; }
        public int? CommunityPopulation { get; set; }
        public string? Acquisition { get; set; }
        public string? Forestry { get; set; }
        public string? ForestryFocus { get; set; }
    }
}
