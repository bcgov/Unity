using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Flattened projection returned by
/// <see cref="IApplicationRepository.GetApplicationListRecordsAsync"/>.
/// Only the columns required for the application list view are selected
/// </summary>
public class ApplicationListRecord
{
    public string? AiAnalysis { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string ReferenceNo { get; init; } = string.Empty;
    public decimal RequestedAmount { get; init; }
    public decimal TotalProjectBudget { get; init; }
    public string? EconomicRegion { get; init; }
    public string? City { get; init; }
    public DateTime? ProposalDate { get; init; }
    public DateTime SubmissionDate { get; init; }
    public DateTime? FinalDecisionDate { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? NotificationDate { get; init; }
    public string? ProjectSummary { get; init; }
    public int? TotalScore { get; init; }
    public decimal RecommendedAmount { get; init; }
    public decimal ApprovedAmount { get; init; }
    public string? LikelihoodOfFunding { get; init; }
    public string? DueDiligenceStatus { get; init; }
    public string? SubStatus { get; init; }
    public string? DeclineRational { get; init; }
    public string? Notes { get; init; }
    public string? AssessmentResultStatus { get; init; }
    public DateTime? AssessmentResultDate { get; init; }
    public DateTime? ProjectStartDate { get; init; }
    public DateTime? ProjectEndDate { get; init; }
    public double? PercentageTotalProjectBudget { get; init; }
    public decimal? ProjectFundingTotal { get; init; }
    public string? Community { get; init; }
    public int? CommunityPopulation { get; init; }
    public string? Acquisition { get; init; }
    public string? Forestry { get; init; }
    public string? ForestryFocus { get; init; }
    public string? ElectoralDistrict { get; init; }
    public string? ApplicantElectoralDistrict { get; init; }
    public string? Place { get; init; }
    public string? RegionalDistrict { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? DefaultSiteId { get; init; }
    public string? SigningAuthorityFullName { get; init; }
    public string? SigningAuthorityTitle { get; init; }
    public string? SigningAuthorityEmail { get; init; }
    public string? SigningAuthorityBusinessPhone { get; init; }
    public string? SigningAuthorityCellPhone { get; init; }
    public string? ContractNumber { get; init; }
    public DateTime? ContractExecutionDate { get; init; }
    public string? RiskRanking { get; init; }
    public string? UnityApplicationId { get; init; }

    // ApplicationStatus (always joined)
    public string Status { get; init; } = string.Empty;

    // ApplicationForm (always joined)
    public string Category { get; init; } = string.Empty;

    // Applicant (always joined)
    public Guid ApplicantId { get; init; }
    public string? ApplicantName { get; init; }
    public Guid? ApplicantSupplierId { get; init; }
    public string? ApplicantSector { get; init; }
    public string? ApplicantSubSector { get; init; }
    public string? ApplicantOrgName { get; init; }
    public string? ApplicantNonRegOrgName { get; init; }
    public string? ApplicantOrganizationType { get; init; }
    public string? ApplicantOrgNumber { get; init; }
    public string? ApplicantOrgStatus { get; init; }
    public string? ApplicantBusinessNumber { get; init; }
    public string? ApplicantOrganizationSize { get; init; }
    public string? ApplicantSectorSubSectorIndustryDesc { get; init; }
    public bool? ApplicantRedStop { get; init; }
    public string? ApplicantIndigenousOrgInd { get; init; }
    public int? ApplicantFiscalDay { get; init; }
    public string? ApplicantFiscalMonth { get; init; }
    public string? ApplicantUnityApplicantId { get; init; }

    // ApplicantAgent (left-joined when present)
    public string? ContactFullName { get; init; }
    public string? ContactTitle { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactBusinessPhone { get; init; }
    public string? ContactCellPhone { get; init; }

    //  Owner / Person (left-joined when present) 
    public Guid? OwnerPersonId { get; init; }
    public string? OwnerFullName { get; init; }

    // Collections (correlated subqueries)
    public List<ApplicationTagListItem> Tags { get; init; } = [];
    public List<ApplicationAssignmentListItem> Assignments { get; init; } = [];
    public List<ApplicationLinkListItem> Links { get; init; } = [];
}

public class ApplicationTagListItem
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string? TagName { get; init; }
}

public class ApplicationAssignmentListItem
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid AssigneeId { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public string? Duty { get; init; }
}

public class ApplicationLinkListItem
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid LinkedApplicationId { get; init; }
    public ApplicationLinkType LinkType { get; init; }
}
