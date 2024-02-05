using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationStatusId { get; set; }

    // Navigation Property - Application Status
    public virtual ApplicationStatus ApplicationStatus
    {
        // NOTE: See https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#required-navigation-properties
        set => _applicationStatus = value;
        get => _applicationStatus
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ApplicationStatus));
    }
    private ApplicationStatus? _applicationStatus;

    public string ProjectName { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal TotalProjectBudget { get; set; }
    public string? Sector { get; set; } = null;
    public string? SubSector { get; set; } = null;
    public string? EconomicRegion { get; set; } = null;
    public string? City { get; set; } = null;
    public DateTime? ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime? AssessmentStartDate { get; set; }
    public DateTime? FinalDecisionDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }

    public string? ProjectSummary { get; set; }

    public int? TotalScore { get; set; } = null;

    public decimal RecommendedAmount { get; set; } = 0;

    public decimal ApprovedAmount { get; set; } = 0;

    public string? LikelihoodOfFunding { get; set; }

    public string? DueDiligenceStatus { get; set; }

    public string? SubStatus { get; set; }

    public string? DeclineRational { get; set; }

    public string? Notes { get; set; }

    public string? AssessmentResultStatus { get; set; }

    public DateTime? AssessmentResultDate { get; set; }

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

    public string? CensusSubdivision { get; set; }

    public string? RegionalDistrict { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? OwnerId { get; set; }
}
