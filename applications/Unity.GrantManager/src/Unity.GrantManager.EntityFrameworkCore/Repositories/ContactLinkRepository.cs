using System;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IContactLinkRepository))]
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class ContactLinkRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : EfCoreRepository<GrantTenantDbContext, ContactLink, Guid>(dbContextProvider), IContactLinkRepository
    {
    }
}
