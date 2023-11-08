
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget
{
    public class SummaryWidgetViewModel
    {        
        public string? Category { get; set; }
        public string? SubmissionDate { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationNumber { get; set; }
        public string? EconomicRegion { get; set; }
        public string? City { get; set; }
        public string? Community { get; set; }
        [DataType(DataType.Currency)]
        public decimal? RequestedAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? ProjectBudget { get; set; }
        public string? Sector { get; set; }
        public string? Status { get; set; }
        public string? LikelihoodOfFunding { get; set; }
        public string? AssessmentStartDate { get; set; }
        public string? FinalDecisionDate { get; set; }
        public string? TotalScore { get; set; }
        public string? AssessmentResult { get; set; }
        [DataType(DataType.Currency)]
        public decimal? RecommendedAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? ApprovedAmount { get; set; }
        public string? Batch { get; set; }
    }
}
