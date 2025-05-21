using System;
using Unity.GrantManager.Notifications.Settings;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IDynamicUrlRepository))]
    public class DynamicUrlRepository : EfCoreRepository<GrantManagerDbContext, DynamicUrl, Guid>, IDynamicUrlRepository
    {
        public DynamicUrlRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
