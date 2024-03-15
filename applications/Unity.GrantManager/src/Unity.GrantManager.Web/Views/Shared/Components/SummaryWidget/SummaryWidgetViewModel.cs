
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget
{
    public class SummaryWidgetViewModel
    {
        // Application

        [Display(Name = "Summary:Application.Category")]
        public string? Category { get; set; }

        [Display(Name = "Summary:Application.SubmissionDate")]
        public string? SubmissionDate { get; set; }

        [Display(Name = "Summary:Application.OrganizationName")]
        public string? OrganizationName { get; set; }

        [Display(Name = "Summary:Application.OrganizationNumber")]
        public string? OrganizationNumber { get; set; }

        [Display(Name = "Summary:Application.EconomicRegion")]
        public string? EconomicRegion { get; set; }

        [Display(Name = "Summary:Application.RegionalDistrict")]
        public string? RegionalDistrict { get; set; }

        [Display(Name = "Summary:Application.Community")]
        public string? Community { get; set; }

        [Display(Name = "Summary:Application.RequestedAmount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal RequestedAmount { get; set; }

        [Display(Name = "Summary:Application.ProjectBudget")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ProjectBudget { get; set; }

        [Display(Name = "Summary:Application.Sector")]
        public string? Sector { get; set; }

        // Assessment

        [Display(Name = "Summary:Assessment.Status")]
        public string? Status { get; set; }

        [Display(Name = "Summary:Assessment.LikelihoodOfFunding")]
        public string? LikelihoodOfFunding { get; set; }

        [Display(Name = "Summary:Assessment.AssessmentStartDate")]
        public string? AssessmentStartDate { get; set; }

        [Display(Name = "Summary:Assessment.FinalDecisionDate")]
        public string? FinalDecisionDate { get; set; }

        [Display(Name = "Summary:Assessment.TotalScore")]
        public int TotalScore { get; set; }

        [Display(Name = "Summary:Assessment.AssessmentResult")]
        public string? AssessmentResult { get; set; }

        [Display(Name = "Summary:Assessment.RecommendedAmount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal RecommendedAmount { get; set; } = 0m;

        [Display(Name = "Summary:Assessment.ApprovedAmount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ApprovedAmount { get; set; }

        [Display(Name = "Summary:Assessment.Batch")]
        public string? Batch { get; set; }

        [Display(Name = "Summary:Application.Assignees")]
        public List<GrantApplicationAssigneeDto> Assignees { get; set; } = new();

        [Display(Name = "Summary:Application.Owner")]
        public GrantApplicationAssigneeDto Owner { get; set; } = new();

        [HiddenInput]
        public Guid ApplicationId { get; set; }
        
        [HiddenInput]
        public Boolean IsReadOnly { get; set; } = false;

    }
}
