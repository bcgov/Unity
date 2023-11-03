using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>
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
    public double RequestedAmount { get; set; } // TODO: change to decimal
    public double TotalProjectBudget { get; set; } // TODO: change to decimal
    public string? Sector { get; set; } = null;
    public string? EconomicRegion { get; set;} = null;
    public string? City { get; set; } = null;
    public DateTime? ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime? AssessmentStartDate { get; set; }
    public DateTime? FinalDecisionDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }

    public string? ProjectSummary { get; set; }  

    public decimal? TotalScore { get; set; } = null; 

    public decimal RecommendedAmount { get; set; } = 0;

    public decimal ApprovedAmount { get; set; } = 0;

    public string? LikelihoodOfFunding { get; set; }

    public string? DueDilligenceStatus { get; set; }

    public string? Recommendation { get; set; }

    public string? DeclineRational { get; set; }

    public string? Notes { get; set; }

    public string? AssessmentResultStatus { get; set; }

    public DateTime? AssessmentResultDate { get; set; }

}
