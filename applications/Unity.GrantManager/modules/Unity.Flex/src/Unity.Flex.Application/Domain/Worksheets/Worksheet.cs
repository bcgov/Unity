using System;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
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

        protected Worksheet()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Worksheet(Guid id,
        string name,
        string uiAnchor)
        : base(id)
        {
            Id = id;
            Name = name;
            UIAnchor = uiAnchor;
        }

        public Worksheet AddSection(WorksheetSection section)
        {
            if (Sections.Any(s => s.Name == section.Name))
                throw new BusinessException("Cannot duplicate name");

            section = section.SetOrder((uint)Sections.Count + 1);

            Sections.Add(section);
            return this;
        }
    }
}
