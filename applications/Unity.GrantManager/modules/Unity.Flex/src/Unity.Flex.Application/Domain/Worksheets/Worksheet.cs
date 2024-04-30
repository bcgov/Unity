using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class Worksheet : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual uint Version { get; private set; } = 1;
        public string UIAnchor { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public virtual Collection<WorksheetSection> Sections { get; private set; } = [];
        public virtual Collection<WorksheetInstance> Instances { get; private set; } = [];

        protected Worksheet()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Worksheet(Guid id,
        string name)
        : base(id)
        {
            Id = id;
            Name = name;
        }
    }
}
