using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex;

public class FlexDataSeedContributor : IDataSeedContributor, ITransientDependency
{
#pragma warning disable S4487 // Unread "private" fields should be removed
    private readonly IGuidGenerator _guidGenerator;
#pragma warning restore S4487 // Unread "private" fields should be removed
    private readonly ICurrentTenant _currentTenant;

    public FlexDataSeedContributor(
        IGuidGenerator guidGenerator, ICurrentTenant currentTenant)
    {
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
    }

    public Task SeedAsync(DataSeedContext context)
    {
        /* Instead of returning the Task.CompletedTask, you can insert your test data
         * at this point!
         */

        using (_currentTenant.Change(context?.TenantId))
        {
            return Task.CompletedTask;
        }
    }
}
