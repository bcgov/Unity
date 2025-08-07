using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.EmailGroups;

public class EmailGroupUser : AuditedAggregateRoot<Guid>, IMultiTenant
{

    public virtual Guid? TenantId { get; protected set; }
    public Guid GroupId  { get; set; } 
    public Guid UserId { get; set; } 
  
}