using System;
using System.Collections.Generic;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationDto : AuditedEntityDto<Guid>
{
    public int RowCount { get; set; } = 0;
    public string ProjectName { get; set; } = string.Empty;
    public GrantApplicationApplicantDto Applicant { get; set; } = new();
    public ApplicationFormDto ApplicationForm { get; set; } = new();
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
    public string? Sector { get; set; } = string.Empty;
    public string? SubSector { get; set; } = string.Empty;

    public int AssessmentCount { get; set; } = 0;
    public int AssessmentReviewCount { get; set; } = 0;

    public string ProjectSummary { get; set; } = string.Empty;
    public int TotalScore { get; set; } = 0;
    public decimal RecommendedAmount { get; set; } = 0;
    public decimal ApprovedAmount { get; set; } = 0;
    public string LikelihoodOfFunding { get; set; } = string.Empty;
    public string DueDiligenceStatus { get; set; } = string.Empty;
    public string SubStatus { get; set; } = string.Empty;
    public string SubStatusDisplayValue { get; set; } = string.Empty;
    public string DeclineRational { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string AssessmentResultStatus { get; set; } = string.Empty;
    public DateTime AssessmentResultDate { get; set; }
    public GrantApplicationState StatusCode { get; set; }
    public DateTime? FinalDecisionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? NotificationDate { get; set; }
    public DateTime? ProjectStartDate { get; set; }
    public DateTime? ProjectEndDate { get; set; }
    public double? PercentageTotalProjectBudget { get; set; }
    public decimal? ProjectFundingTotal { get; set; }
    public string? Community { get; set; }
    public int? CommunityPopulation { get; set; }
    public string? Acquisition { get; set; }
    public string? Forestry { get; set; }
    public string? ForestryFocus { get; set; }
    public string? ElectoralDistrict { get; set; }
    public string? RegionalDistrict { get; set; }
    public string? ContactFullName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactBusinessPhone { get; set; }
    public string? ContactCellPhone { get; set; }
    public string? ApplicationTag { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationType { get; set; }
    public GrantApplicationAssigneeDto Owner { get; set; } = new();
    public string? OrgStatus  { get; set; } = string.Empty;
    public string? OrganizationSize { get; set; } = string.Empty;
    public string? OrgNumber { get; set; } = string.Empty;
    public string? SectorSubSectorIndustryDesc { get; set; } = string.Empty;
    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }
    public string? ContractNumber { get; set; }
    public DateTime? ContractExecutionDate { get; set; }
    public string? Place {  get; set; }
    public string? RiskRanking  { get; set;}
}
