using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Flex.Domain.WorksheetLinks;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Unity.Flex.Worksheets;
using Unity.Flex.Reporting;

namespace Unity.Flex.Domain.Worksheets
{
    public class Worksheet : FullAuditedAggregateRoot<Guid>, IMultiTenant, IReportableEntity<Worksheet>
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Title { get; private set; } = string.Empty;
        public virtual uint Version { get; private set; } = 1;
        public virtual bool Published { get; private set; } = false;

        public Guid? TenantId { get; set; }

        public virtual Collection<WorksheetSection> Sections { get; private set; } = [];
        public virtual Collection<WorksheetLink> Links { get; private set; } = [];

        // For reporting purposes
        public virtual string ReportColumns { get; set; } = string.Empty;
        public virtual string ReportKeys { get; set; } = string.Empty;
        public virtual string ReportViewName { get; set; } = string.Empty;

        protected Worksheet()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Worksheet(Guid id,
            string name,
            string title)
        : base(id)
        {
            Id = id;
            Name = name.SanitizeWorksheetName();
            Title = title;
        }

        public Worksheet AddSection(WorksheetSection section)
        {
            if (Sections.Any(s => s.Name == section.Name))
                throw new UserFriendlyException("Section names must be unique");

            section = section.SetOrder((uint)Sections.Count + 1);

            Sections.Add(section);
            return this;
        }

        internal Worksheet CloneSection(WorksheetSection clonedSection)
        {
            clonedSection = clonedSection.SetOrder((uint)Sections.Count + 1);

            Sections.Add(clonedSection);
            return this;
        }

        public Worksheet UpdateSection(WorksheetSection section, string name)
        {
            section.SetName(name);
            return this;
        }

        public Worksheet SetTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new UserFriendlyException("Blank titles are not allowed");

            Title = title;
            return this;
        }

        public Worksheet SetVersion(uint version)
        {
            Version = version;
            return this;
        }

        public Worksheet SetPublished(bool published)
        {
            Published = published;
            return this;
        }

        public Worksheet RemoveSection(WorksheetSection section)
        {
            Sections.Remove(section);
            return this;
        }

        public Worksheet SetName(string name)
        {
            Name = name;
            return this;
        }

        public Worksheet SetReportingFields(string keys, string columns, string reportViewName)
        {
            ReportKeys = keys;
            ReportColumns = columns;
            ReportViewName = reportViewName;
            return this;
        }
    }
}
