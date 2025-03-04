using Unity.Reporting.Domain;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore;

[ConnectionStringName(ReportingDbProperties.ConnectionStringName)]
public interface IReportingDbContext : IEfCoreDbContext
{
}
