using System;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateProjectInfoDto : CustomDataFieldDto
    {
        public Guid? ApplicationId { get; set; }
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
        public string? RegionalDistrict { get; set; }
        public string? ContactFullName { get; set; }
        public string? ContactTitle { get; set;}
        public string? ContactEmail { get; set;}
        public string? ContactBusinessPhone { get; set;}
        public string? ContactCellPhone { get; set;}
        public string? SigningAuthorityFullName { get; set; }
        public string? SigningAuthorityTitle { get; set; }
        public string? SigningAuthorityEmail { get; set; }
        public string? SigningAuthorityBusinessPhone { get; set; }
        public string? SigningAuthorityCellPhone { get; set; }
        public string? ContractNumber { get; set; }
        public DateTime? ContractExecutionDate { get; set; }
        public string? Place {  get; set; }
    }
}
