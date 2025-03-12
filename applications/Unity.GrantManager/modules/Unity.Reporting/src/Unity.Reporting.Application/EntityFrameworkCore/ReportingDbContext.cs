using Microsoft.EntityFrameworkCore;
using Unity.Reporting.Domain;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore;

[ConnectionStringName(ReportingDbProperties.ConnectionStringName)]
public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : AbpDbContext<ReportingDbContext>(options), IReportingDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureReporting();
    }
}
