using Microsoft.EntityFrameworkCore;
using Unity.Reporting.Domain;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore;

[ConnectionStringName(ReportingDbProperties.ConnectionStringName)]
public interface IReportingDbContext : IEfCoreDbContext
{
    public DbSet<ReportColumnsMap> ReportColumnsMaps { get; set; }
}
