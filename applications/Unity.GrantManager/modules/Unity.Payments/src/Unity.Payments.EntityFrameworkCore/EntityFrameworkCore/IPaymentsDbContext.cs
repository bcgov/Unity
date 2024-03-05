using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public interface IPaymentsDbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */
}
