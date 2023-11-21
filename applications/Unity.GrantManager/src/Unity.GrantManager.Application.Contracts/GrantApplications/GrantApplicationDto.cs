using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationDto : AuditedEntityDto<Guid>
{
    public string ProjectName { get; set; } = string.Empty;
    public string Applicant { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public List<GrantApplicationAssigneeDto> Assignees { get; set; } = new();
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateTime ProposalDate { get; set; }

    public string ApplicationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EconomicRegion { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal TotalProjectBudget { get; set; }
    public string Sector { get; set; } = string.Empty;

    public int AssessmentCount { get; set; } = 0;
    public int AssessmentReviewCount { get; set; } = 0;

    public string ProjectSummary { get; set; } = string.Empty;
    public int TotalScore { get; set; } = 0;
    public decimal RecommendedAmount { get; set; } = 0;
    public decimal ApprovedAmount { get; set; } = 0;
    public string LikelihoodOfFunding { get; set; } = string.Empty;
    public string DueDilligenceStatus { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string DeclineRational { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string AssessmentResultStatus { get; set; } = string.Empty;
    public DateTime AssessmentResultDate { get; set; }
    public GrantApplicationState StatusCode { get; set; }
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
