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
        public double? PercentageTotalProjectBudget { get; set; }
        public decimal? ProjectFundingTotal { get; set; }
        public string? Community { get; set; }
        public int? CommunityPopulation { get; set; }
        public string? Acquisition { get; set; }
        public string? Forestry { get; set; }
        public string? ForestryFocus { get; set; }
        public string? Sector { get; set; }
        public string? SubSector { get; set; }
        public string? ElectoralDistrict { get; set; }
        public string? EconomicRegion { get; set; }
        public string? CensusSubdivision { get; set; }
        public string? RegionalDistrict { get; set; }
    }
}
