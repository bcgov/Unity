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
public class FlexDbContext(DbContextOptions<FlexDbContext> options) : AbpDbContext<FlexDbContext>(options), IFlexDbContext
{
    // Worksheets
    public DbSet<Worksheet> Worksheets { get; set; }
    public DbSet<WorksheetLink> WorksheetLinks { get; set; }
    public DbSet<WorksheetInstance> WorksheetsInstances { get; set; }
    public DbSet<WorksheetSection> WorksheetsSections { get; set; }
    public DbSet<CustomField> CustomFields { get; set; }
    public DbSet<CustomFieldValue> CustomFieldValues { get; set; }    

    // Scoresheets
    public DbSet<Scoresheet> Scoresheets { get; set; }
    public DbSet<ScoresheetInstance> ScoresheetInstances { get; set; }
    public DbSet<ScoresheetSection> ScoresheetSections { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureFlex();
    }
}
