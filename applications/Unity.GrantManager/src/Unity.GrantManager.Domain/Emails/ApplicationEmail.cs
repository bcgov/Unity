using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Emails
{
    public abstract class ApplicationEmail : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public string Email { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
    }
}
