using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Intakes;

public class Intake : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public double Budget { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string IntakeName { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }
}
