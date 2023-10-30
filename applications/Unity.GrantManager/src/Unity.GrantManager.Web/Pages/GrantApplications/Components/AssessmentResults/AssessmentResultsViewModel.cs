using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace Unity.GrantManager.Web.Pages.GrantApplications.Components.AssessmentResults
{

    public class AssessmentResultsPageModel : PageModel
    {

        public List<SelectListItem> FundingRiskList = new List<SelectListItem>
            {
                new SelectListItem { Value = "LOW", Text = "Low"},
                new SelectListItem { Value = "MEDIUM", Text = "Medium"},
                new SelectListItem { Value = "HIGH", Text = "High"},
            };

        public List<SelectListItem> DueDilligenceList = new List<SelectListItem>
            {
                new SelectListItem { Value = "COMPLETE", Text = "Complete"},
                new SelectListItem { Value = "UNDERWAY", Text = "Underway"},
                new SelectListItem { Value = "PAUSED", Text = "Paused"},
                new SelectListItem { Value = "WITHDRAWN", Text = "Withdrawn"},
                new SelectListItem { Value = "INELIGIBLE", Text = "Ineligible"},
                new SelectListItem { Value = "FAILED", Text = "Failed"},
            };

        public List<SelectListItem> AssessmentResultStatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "PASS", Text = "Pass"},
                new SelectListItem { Value = "FAIL", Text = "Fail"},
                new SelectListItem { Value = "INELIGIBLE", Text = "Ineligible"},
            };


        public List<SelectListItem> RecommendationActionList = new List<SelectListItem>
            {
                new SelectListItem { Value = "APPROVE", Text = "Recommended for Approval"},
                new SelectListItem { Value = "DENY", Text = "Recommended for Denial"}
            };

        public List<SelectListItem> DeclineRationalActionList = new List<SelectListItem>
        {
            new SelectListItem { Value = "NO_READINESS", Text = "Lack of readiness"},
            new SelectListItem { Value = "LOW_PRIORITY", Text = "Lower priority relative to other requests"},
            new SelectListItem { Value = "NOT_ENOUGH_INFO", Text = "Insufficient information provided"},
            new SelectListItem { Value = "INELIGIBLE_PROJECT", Text = "Ineligible Project"},
            new SelectListItem { Value = "INELIGIBLE_APPLICANT", Text = "Ineligible Applicant"},
            new SelectListItem { Value = "INSUFFICIENT_READINESS", Text = "Insufficient Readiness"},
            new SelectListItem { Value = "SMALL_PROJECT", Text = "Project too small"},
            new SelectListItem { Value = "DENY", Text = "Other"},
        };

        public Guid ApplicationId { get; set; }

        public AssessmentResultsModel AssessmentResults { get; set; }

        public void onGet()
        {
            AssessmentResults = new AssessmentResultsModel();

        }

        public class AssessmentResultsModel
        {

            [TextArea(Rows = 1)]
            public string? ProjectSummary { get; set; }

            public decimal? TotalScore { get; set; }

            public decimal? RequestedAmount { get; set; }

            public double? TotalProjectBudget { get; set; }

            public decimal? RecommendedAmount { get; set; }

            public decimal? ApprovedAmount { get; set; }

            [SelectItems(nameof(FundingRiskList))]
            public string? LikelihoodOfFunding { get; set; }

            [SelectItems(nameof(DueDilligenceList))]
            public string? DueDilligenceStatus { get; set; }

            [SelectItems(nameof(RecommendationActionList))]
            public string? Recommendation { get; set; }

            [SelectItems(nameof(DeclineRationalActionList))]
            public string? DeclineRational { get; set; }

            [TextArea(Rows = 2)]
            public string? Notes { get; set; }

            [SelectItems(nameof(AssessmentResultStatusList))]
            public string? AssessmentResultStatus { get; set; }

        }
    }

    
}

