using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationLink : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public Guid LinkedApplicationId { get; set; }
    public Guid? TenantId { get; set; }
}
