using System;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IRegionalDistrictRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class RegionalDistrictRepository : EfCoreRepository<GrantManagerDbContext, RegionalDistrict, Guid>, IRegionalDistrictRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public RegionalDistrictRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
