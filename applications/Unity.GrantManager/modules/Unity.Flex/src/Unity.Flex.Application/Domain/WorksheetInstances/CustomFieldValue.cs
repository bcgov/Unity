using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class CustomFieldValue : FullAuditedEntity<Guid>, IMultiTenant
    {
        [Column(TypeName = "jsonb")]
        public virtual string? CurrentValue { get; private set; } = "{}";

        [Column(TypeName = "jsonb")]
        public virtual string? Definition { get; private set; } = "{}";

        public Guid? TenantId { get; set; }

        public Guid WorksheetInstanceId { get; }
        public Guid CustomFieldId { get; }
        public Guid SectionId { get; }
    }
}
