using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Comments
{
    public abstract class CommentBase : AuditedAggregateRoot<Guid>
    {        
        public string Comment { get; set; } = string.Empty;
        public string Commenter { get; set; } = string.Empty;        
    }
}
