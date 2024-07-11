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
    public class ApplicationFormVersionRepository : EfCoreRepository<GrantTenantDbContext, ApplicationFormVersion, Guid>, IApplicationFormVersionRepository
    {
        public ApplicationFormVersionRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<ApplicationFormVersion> GetByChefsFormVersionAsync(Guid chefsFormVersionId)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.ApplicationFormVersions
                .FirstAsync(s => s.ChefsFormVersionGuid == chefsFormVersionId.ToString());
        }
    }
}