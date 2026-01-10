using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IPersonRepository))]
    public class PersonRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : EfCoreRepository<GrantTenantDbContext, Person, Guid>(dbContextProvider), IPersonRepository
    {
        public async Task<Person?> FindByOidcSub(string oidcSub)
        {
            var dbSet = await GetDbSetAsync();
            var compare = oidcSub.ToSubjectWithoutIdp().ToUpper();
            return await dbSet.AsQueryable()
                .FirstOrDefaultAsync(s => s.OidcSub.StartsWith(compare));
        }
    }
}
