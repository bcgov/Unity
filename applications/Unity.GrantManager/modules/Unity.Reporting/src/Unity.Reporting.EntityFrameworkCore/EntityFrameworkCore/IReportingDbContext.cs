using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore;

[ConnectionStringName(ReportingDbProperties.ConnectionStringName)]
public interface IReportingDbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */
}
