using System;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class WorksheetSection : FullAuditedEntity<Guid>, IMultiTenant
    {
        // Navigation
        public virtual Guid WorksheetId { get; set; }
        public virtual Worksheet Worksheet
        {
            set => _worksheet = value;
            get => _worksheet
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Worksheet));
        }
        private Worksheet? _worksheet;


        public virtual string Name { get; private set; } = string.Empty;
        public virtual uint Order { get; private set; }

        public virtual Collection<CustomField> Fields { get; private set; } = [];

        public Guid? TenantId { get; set; }

        protected WorksheetSection()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public WorksheetSection(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public WorksheetSection SetName(string name)
        {
            if (Worksheet.Sections.Any(s => s.Name == name && s.Id != Id))
                throw new UserFriendlyException("Cannot duplicate section names.");

            Name = name;
            return this;
        }

        public WorksheetSection AddField(CustomField field)
        {
            if (Worksheet.Sections.SelectMany(s => s.Fields).Any(s => s.Name == field.Name && s.Id != field.Id))
                throw new UserFriendlyException("Cannot duplicate field names for a worksheet.");

            field = field.SetOrder((uint)Fields.Count + 1);

            Fields.Add(field);
            return this;
        }

        internal WorksheetSection CloneField(CustomField clonedField)
        {
            clonedField = clonedField.SetOrder((uint)Fields.Count + 1);

            Fields.Add(clonedField);
            return this;
        }

        public WorksheetSection SetOrder(uint order)
        {
            Order = order;
            return this;
        }

        public WorksheetSection RemoveField(CustomField field)
        {
            Fields.Remove(field);
            return this;
        }        
    }
}
