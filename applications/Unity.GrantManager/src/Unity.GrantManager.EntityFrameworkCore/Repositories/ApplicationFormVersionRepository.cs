using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{

    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IApplicationFormVersionRepository))]
    public class ApplicationFormVersionRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : EfCoreRepository<GrantTenantDbContext, ApplicationFormVersion, Guid>(dbContextProvider), IApplicationFormVersionRepository
    {
        public async Task<ApplicationFormVersion?> GetByChefsFormVersionAsync(Guid chefsFormVersionId)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.ApplicationFormVersions
                .FirstOrDefaultAsync(s => s.ChefsFormVersionGuid == chefsFormVersionId.ToString());
        }
    }
}