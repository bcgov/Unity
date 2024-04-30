using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class CustomField : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; private set; } = string.Empty;
        public virtual bool Enabled { get; private set; }
        public Guid? TenantId { get; set; }

        // Navigation
        public virtual WorksheetSection? Section { get; }
        public virtual Guid SectionId { get; }

        public virtual Collection<CustomFieldValue> Values { get; private set; } = [];
    }
}
