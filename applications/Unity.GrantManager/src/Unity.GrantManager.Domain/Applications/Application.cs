using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationStatusId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public double EligibleAmount { get; set; }
    public double RequestedAmount { get; set; }
    public DateTime ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }      

}
