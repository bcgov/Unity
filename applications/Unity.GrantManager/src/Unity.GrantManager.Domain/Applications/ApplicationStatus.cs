using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationStatus : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string ExternalStatus { get; set; } = string.Empty;

    public string InternalStatus { get; set; } = string.Empty;

    public GrantApplicationState StatusCode { get; set; }

    // Navigation Property
    [JsonIgnore]
    public virtual ICollection<Application>? Applications { get; set; }

    public Guid? TenantId { get; set; }
}