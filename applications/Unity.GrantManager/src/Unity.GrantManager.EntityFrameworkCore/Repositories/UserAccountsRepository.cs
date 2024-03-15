using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IUserAccountsRepository))]
    public class UserAccountsRepository : EfCoreRepository<GrantManagerDbContext, IdentityUser, Guid>, IUserAccountsRepository
    {
        public UserAccountsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<IList<IdentityUser>> GetListByOidcSub(string oidcSub)
        {
            var dbSet = await GetDbSetAsync();            
            return dbSet.AsQueryable().Where(u => EF.Property<string>(u, "OidcSub")
                .ToUpper()
                .StartsWith(oidcSub.ToSubjectWithoutIdp()))
                .ToList();
        }
    }
}
