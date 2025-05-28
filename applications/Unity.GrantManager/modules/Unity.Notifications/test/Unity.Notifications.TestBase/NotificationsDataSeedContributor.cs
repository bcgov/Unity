using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Notifications.Templates;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications;

public class NotificationsDataSeedContributor : IDataSeedContributor, ITransientDependency
{    
    private readonly ICurrentTenant _currentTenant;

    public NotificationsDataSeedContributor(ICurrentTenant currentTenant)
    {        
        _currentTenant = currentTenant;
    }

    public  Task SeedAsync(DataSeedContext context)
    {
        using (_currentTenant.Change(context?.TenantId))
        {
            return Task.CompletedTask;
        }





    }
}
