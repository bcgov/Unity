using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting;

public class ReportingDataSeedContributor(ICurrentTenant currentTenant) : IDataSeedContributor, ITransientDependency
{
    public Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(context?.TenantId))
        {
            // Insert any test data
            return Task.CompletedTask;
        }
    }
}
