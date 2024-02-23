using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public class GetSummaryDto
{
    public string? Category { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationNumber { get; set; }
    public string? EconomicRegion { get; set; }
    public string? City { get; set; }
    public string? Community { get; set; }
    public decimal? RequestedAmount { get; set; }
    public decimal? ProjectBudget { get; set; }
    public string? Sector { get; set; }
    public string? Status { get; set; }
    public string? LikelihoodOfFunding { get; set; }
    public string? AssessmentStartDate { get; set; }
    public string? FinalDecisionDate { get; set; }
    public string? TotalScore { get; set; }
    public string? AssessmentResult { get; set; }
    public decimal? RecommendedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? Batch { get; set; }
    public string? RegionalDistrict { get; set; }
    public Guid? OwnerId { get; set; }
    public List<GrantApplicationAssigneeDto> Assignees { get; set; } = new();
    public GrantApplicationAssigneeDto Owner { get; set; } = new();
}
