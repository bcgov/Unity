using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationFormVersion : FullAuditedAggregateRoot<Guid>
{
    public Guid ApplicationFormId { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsFormVersionGuid { get; set; }
    public string? SubmissionHeaderMapping { get; set; }
    public string? AvailableChefsFields { get; set; }
    public int? Version { get; set; }
    public bool Published { get; set; }
}
