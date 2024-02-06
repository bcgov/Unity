using System;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICommunityRepository))]
    public class CommunityRepository : EfCoreRepository<GrantManagerDbContext, Community, Guid>, ICommunityRepository
    {
        public CommunityRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
