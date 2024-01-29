using System;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
// This pattern is an implementation ontop of ABP framework, will not change this

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ITenantTokenRepository))]
    public class TenantTokenRepository : EfCoreRepository<GrantManagerDbContext, TenantToken, Guid>, ITenantTokenRepository
    {
        public TenantTokenRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
