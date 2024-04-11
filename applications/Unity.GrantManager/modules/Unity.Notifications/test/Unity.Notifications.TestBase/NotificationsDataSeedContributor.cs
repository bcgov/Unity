using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications;

public class NotificationsDataSeedContributor : IDataSeedContributor, ITransientDependency
{    
    private readonly ICurrentTenant _currentTenant;

    public NotificationsDataSeedContributor(ICurrentTenant currentTenant)
    {        
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
