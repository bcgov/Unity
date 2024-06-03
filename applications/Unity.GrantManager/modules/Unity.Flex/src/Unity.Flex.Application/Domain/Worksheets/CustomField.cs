using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class CustomField : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; private set; } = string.Empty;
        public virtual CustomFieldType Type { get; private set; } = CustomFieldType.Undefined;
        public virtual uint Order { get; private set; }
        public virtual bool Enabled { get; private set; } = true;

        [Column(TypeName = "jsonb")]
        public virtual string Definition { get; private set; } = "{}";

        public Guid? TenantId { get; set; }

        // Navigation        
        public virtual WorksheetSection Section
        {
            set => _section = value;
            get => _section
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Section));
        }
        public virtual Guid SectionId { get; }
        private WorksheetSection? _section;

        protected CustomField()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public CustomField(Guid id, string name, string worksheetName, string label, CustomFieldType type, string? definition)
        {
            Id = id;
            Name = ConfigureName(name, worksheetName);
            Label = label;
            Type = type;
            Definition = definition ?? DefinitionResolver.Resolve(type);
        }

        private static string ConfigureName(string name, string worksheetName)
        {
            return "custom_" + SanitizeNameField(worksheetName) + "_" + SanitizeNameField(name);
        }

        public CustomField SetName(string name)
        {
            if (Section.Fields.Any(s => s.Name == name))
                throw new BusinessException("Cannot duplicate name");

            Name = name;
            return this;
        }

        public CustomField SetLabel(string label)
        {
            Label = label;
            return this;
        }

        public CustomField SetEnabled(bool enabled)
        {
            Enabled = enabled;
            return this;
        }

        public CustomField SetOrder(uint order)
        {
            Order = order;
            return this;
        }

        private static string SanitizeNameField(string field)
        {
            return field.Trim().ToLower().Replace(" ", "");
        }
    }
}
