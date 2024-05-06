using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class WorksheetSection : FullAuditedEntity<Guid>, IMultiTenant
    {
        // Navigation
        public virtual Worksheet? Worksheet { get; }
        public virtual Guid WorksheetId { get; }

        public virtual Collection<CustomField> Fields { get; private set; } = [];


        public Guid? TenantId { get; set; }
    }
}
