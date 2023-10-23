using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Pages.GrantApplications.Components.AssessmentResults
{
    public enum FundingRiskList
    {
        Low,
        Medium,
        High
    }

    public enum DueDilligenceList
    {
        Complete,
        Underway,
        Paused,
        Withdrawn,
        Ineligible,
        Failed
    }

    public enum AssessmentResultStatusList
    {
        Pass,
        Fail,
        Ineligible
    };



    public class AssessmentResultsViewModel
	{
        [TextArea(Rows = 1)]
        public string? ProjectSummary { get; set; }

        public double? TotalScore { get; set; }

        public double? RequestedAmount { get; set; }

        public double? TotalProjectBudget { get; set; }

        public double? RecommendedAmount { get; set; }

        public double? ApprovedAmount { get; set; }

        public FundingRiskList? LikelihoodOfFunding { get; set; }

        public DueDilligenceList? DueDilligenceStatus { get; set; }

        public string? Recommendation { get; set; }

        public string? DeclineRational { get; set; }

        [TextArea(Rows = 2)]
        public string? Notes { get; set; }

        public AssessmentResultStatusList? AssessmentResult { get; set; }

    }
}

