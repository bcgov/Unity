using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.GrantManager.GrantApplications;

public class GetSummaryDto
{
    public string? Category { get; set; }
    public string? SubmissionDate { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationNumber { get; set; }
    public string? EconomicRegion { get; set; }
    public string? City { get; set; }
    public string? Community { get; set; }
    public string? RequestedAmount { get; set; }
    public string? ProjectBudget { get; set; }
    public string? Sector { get; set; }
    public string? Status { get; set; }
    public string? LikelihoodOfFunding { get; set; }
    public string? AssessmentStartDate { get; set; }
    public string? FinalDecisionDate { get; set; }
    public string? TotalScore { get; set; }
    public string? AssessmentResult { get; set; }
    public string? RecommendedAmount { get; set; }
    public string? ApprovedAmount { get; set; }
    public string? Batch { get; set; }
}
