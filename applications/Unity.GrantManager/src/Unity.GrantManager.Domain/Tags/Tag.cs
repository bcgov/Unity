using JetBrains.Annotations;
using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GlobalTag;

public class Tag : AuditedAggregateRoot<Guid>, IMultiTenant
{

    public virtual Guid? TenantId { get; protected set; }
    public  string Name { get;  set; } = string.Empty;

}