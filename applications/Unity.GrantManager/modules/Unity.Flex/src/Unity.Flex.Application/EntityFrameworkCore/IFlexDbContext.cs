using Microsoft.EntityFrameworkCore;
using Unity.Flex.Domain;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore;

[ConnectionStringName(FlexDbProperties.ConnectionStringName)]
public interface IFlexDbContext : IEfCoreDbContext
{
    // Worksheets
    DbSet<Worksheet> Worksheets { get; set; }
    DbSet<WorksheetLink> WorksheetLinks { get; set; }
    DbSet<WorksheetInstance> WorksheetsInstances { get; set; }
    DbSet<WorksheetSection> WorksheetsSections { get; set; }
    DbSet<CustomField> CustomFields { get; set; }
    DbSet<CustomFieldValue> CustomFieldValues { get; set; }

    // Scoresheets
    DbSet<Scoresheet> Scoresheets { get; set; }
    DbSet<ScoresheetInstance> ScoresheetInstances { get; set; }
    DbSet<ScoresheetSection> ScoresheetSections { get; set; }
    DbSet<Question> Questions { get; set; }
    DbSet<Answer> Answers { get; set; }
}

