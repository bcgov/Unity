using System;
using System.Linq;
using System.Text.Json.Serialization;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicantAddress : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    
    [JsonIgnore]
    public virtual Applicant Applicant
    {
        set => _applicant = value;
        get => _applicant
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Applicant));
    }
    private Applicant? _applicant;

    public Guid? ApplicationId { get; set; }

    [JsonIgnore]
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }
    private Application? _application;

    public string? City { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string? Province { get; set; } = string.Empty;
    public string? Postal { get; set; } = string.Empty;
    public string? Street { get; set; } = string.Empty;
    public string? Street2 { get; set; } = string.Empty;
    public string? Unit { get; set; } = string.Empty;
    public AddressType AddressType { get; set; } = AddressType.PhysicalAddress;
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Returns the address as a single comma-separated string, ordered by relevance.
    /// </summary>
    public string GetFullAddress()
    {
        var parts = new[]
        {
            Street,
            Street2,
            Unit,
            City,
            Province,
            Postal,
            Country
        };

        var address = string.Join(", ", parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim()));

        return address;
    }
}
