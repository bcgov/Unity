using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationStatusId { get; set; }
    // Navigation Property
    // TODO: Figure out the correct nullable reference type for a navigation property
    // https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
    public virtual ApplicationStatus ApplicationStatus { get; set; }

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
