using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class CustomFieldValue : FullAuditedEntity<Guid>, IMultiTenant, ICorrelationEntity
    {
        [Column(TypeName = "jsonb")]
        public virtual string? CurrentValue { get; private set; } = "{}";
        [Column(TypeName = "jsonb")]
        public virtual string? DefaultValue { get; private set; } = "{}";

        public uint Version { get; set; } = 1;

        public Guid? TenantId { get; set; }


        // Navigation
        public CustomField? CustomField { get; }
        public Guid CustomFieldId { get; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;        
    }
}
