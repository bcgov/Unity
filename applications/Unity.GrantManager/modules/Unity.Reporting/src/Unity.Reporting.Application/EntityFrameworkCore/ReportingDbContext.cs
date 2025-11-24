using Microsoft.EntityFrameworkCore;
using Unity.Reporting.Domain;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore;

[ConnectionStringName(ReportingDbProperties.ConnectionStringName)]
public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : AbpDbContext<ReportingDbContext>(options), IReportingDbContext
{
    public DbSet<ReportColumnsMap> ReportColumnsMaps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureReporting();
    }
}
