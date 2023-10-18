using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
    public double EligibleAmount { get; set; }
    public double RequestedAmount { get; set; }
    public double TotalProjectBudget { get; set; }
    public string? Sector { get; set; } = null;
    public string? EconomicRegion { get; set;} = null;
    public string? City { get; set; } = null;
    public DateTime? ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }

}
