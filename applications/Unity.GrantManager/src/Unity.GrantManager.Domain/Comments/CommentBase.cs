using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Comments
{
    public abstract class CommentBase : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public string Comment { get; set; } = string.Empty;
        public Guid CommenterId { get; set; }
        public Guid? TenantId { get; set; }
    }
}
