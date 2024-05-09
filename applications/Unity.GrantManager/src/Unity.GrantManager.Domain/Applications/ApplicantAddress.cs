using System;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicantAddress : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    public virtual Applicant Applicant
    {
        set => _applicant = value;
        get => _applicant
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Applicant));
    }
    private Applicant? _applicant;

    public string? City { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string? Province { get; set; } = string.Empty;
    public string? Postal { get; set; } = string.Empty;
    public string? Street { get; set; } = string.Empty;
    public string? Street2 { get; set; } = string.Empty;
    public string? Unit { get; set; } = string.Empty;
    public AddressType AddressType { get; set; } = AddressType.PhysicalAddress;
    public Guid? TenantId { get; set; }
}
