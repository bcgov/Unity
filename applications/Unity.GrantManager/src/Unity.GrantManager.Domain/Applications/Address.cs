using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Address : AuditedAggregateRoot<Guid>
{
    public Guid? ApplicantId { get; set; }
    public string? City { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string? Province { get; set; } = string.Empty;
    public string? Postal { get; set; } = string.Empty;
    public string? Street { get; set; } = string.Empty;
    public string? Street2 { get; set; } = string.Empty;
    public string? Unit { get; set; } = string.Empty;
}
