using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Tokens
{
    public class TenantToken : AuditedEntity<Guid>
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; } = string.Empty;
    }
}
