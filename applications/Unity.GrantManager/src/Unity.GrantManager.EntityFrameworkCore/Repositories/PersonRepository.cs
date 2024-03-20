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
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class PersonRepository : EfCoreRepository<GrantTenantDbContext, Person, Guid>, IPersonRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public PersonRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<Person?> FindByOidcSub(string oidcSub)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AsQueryable()
                .FirstOrDefaultAsync(s => s.OidcSub
                .ToUpper()
                .StartsWith(oidcSub.ToSubjectWithoutIdp()));                
        }
    }
}
