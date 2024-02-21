using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ISectorRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class SectorRepository : EfCoreRepository<GrantManagerDbContext, Sector, Guid>, ISectorRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public SectorRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public override async Task<IQueryable<Sector>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
}
